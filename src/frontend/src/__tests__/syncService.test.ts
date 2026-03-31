import fakeIndexedDB from "fake-indexeddb";
import { db } from "@/db/gymflow.db";
import { syncService } from "@/services/syncService";
import { fetchWithAuth } from "@/services/httpClient";
import { vi } from "vitest";

vi.mock("@/services/httpClient", () => ({
  fetchWithAuth: vi.fn(),
}));

const mockFetch = vi.mocked(fetchWithAuth);

describe("syncService", () => {
  beforeAll(() => {
    global.indexedDB = fakeIndexedDB;
    vi.spyOn(db, "open").mockResolvedValue(db as any); // evitar initDatabase
  });

  beforeEach(async () => {
    Object.defineProperty(navigator, "onLine", { value: true, writable: true });
    await db.metadata.put({ key: "syncLock", value: "false" }); // Reset sync lock
  });

  afterEach(async () => {
    await db.sync_queue.clear();
    await db.error_queue.clear();
    await db.users.clear();
    await db.metadata.clear();
  });

  it("should not process queue when offline", async () => {
    Object.defineProperty(navigator, "onLine", { value: false });

    await syncService.processQueue();

    // Verify no processing occurred
    const syncItems = await db.sync_queue.toArray();
    expect(syncItems).toHaveLength(0);

    const errorItems = await db.error_queue.toArray();
    expect(errorItems).toHaveLength(0);
  });

  it("should skip processing when queue is empty", async () => {
    await syncService.processQueue();

    // Verify no fetch calls and no changes
    expect(mockFetch).not.toHaveBeenCalled();
  });

  it("should successfully process a sync_queue item", async () => {
    // Prepare sync_queue item
    const queueItem = {
      guid: "test-guid-1",
      type: "CheckIn",
      payload: JSON.stringify({ memberId: "abc" }),
      timestamp: Date.now(),
      retryCount: 0,
      isOffline: true,
    };
    await db.sync_queue.add(queueItem);

    // Mock API response
    const apiResponse = { id: "abc", status: "Active" };
    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(apiResponse),
    } as Response);

    await syncService.processQueue();

    // Ensure item is removed from sync_queue
    const queue = await db.sync_queue.toArray();
    expect(queue).toHaveLength(0);

    // Verify API call
    expect(mockFetch).toHaveBeenCalledWith("/api/checkin", expect.objectContaining({
      method: "POST",
    }));

    // Ensure local cache updated
    const user = await db.users.get("abc");
    expect(user).toMatchObject({ status: "Active" });
  });

  it("should dispatch sync:auth-required on API 401", async () => {
    // Prepare sync_queue item
    const queueItem = {
      guid: "test-guid-2",
      type: "CheckIn",
      payload: JSON.stringify({ memberId: "xyz" }),
      timestamp: Date.now(),
      retryCount: 0,
      isOffline: true,
    };
    await db.sync_queue.add(queueItem);

    // Mock API response
    mockFetch.mockResolvedValue({ ok: false, status: 401 } as Response);

    const dispatchSpy = vi.spyOn(window, "dispatchEvent");

    await syncService.processQueue();

    expect(dispatchSpy).toHaveBeenCalledWith(new CustomEvent("sync:auth-required"));

    // Ensure item remains in sync_queue
    const queue = await db.sync_queue.toArray();
    expect(queue).toHaveLength(1);
  });

  it("should move item to error_queue after max retries", async () => {
    // Prepare sync_queue item with max retries
    const queueItem = {
      guid: "test-guid-3",
      type: "CheckIn",
      payload: JSON.stringify({ memberId: "uvw" }),
      timestamp: Date.now(),
      retryCount: 2, // MAX_RETRIES - 1
      isOffline: true,
    };
    await db.sync_queue.add(queueItem);

    // Mock API failure
    mockFetch.mockResolvedValue({ ok: false, status: 500 } as Response);

    await syncService.processQueue();

    // Ensure item moved to error_queue
    const errorItems = await db.error_queue.toArray();
    expect(errorItems).toHaveLength(1);
    expect(errorItems[0]).toMatchObject({
      guid: "test-guid-3",
      type: "CheckIn",
    });

    // Ensure item removed from sync_queue
    const queue = await db.sync_queue.toArray();
    expect(queue).toHaveLength(0);
  });

  it("should retry from error_queue", async () => {
    // Prepare error_queue item
    const errorItem = {
      guid: "test-guid-4",
      type: "CheckIn",
      payload: JSON.stringify({ someData: true }),
      timestamp: Date.now(),
      retryCount: 3,
      lastError: "test-error",
      failedAt: Date.now(),
    };
    await db.error_queue.add(errorItem);

    await syncService.retryFromErrorQueue("test-guid-4");

    // Verify item moved to sync_queue
    const syncItems = await db.sync_queue.toArray();
    expect(syncItems).toHaveLength(1);
    expect(syncItems[0]).toMatchObject({
      guid: "test-guid-4",
      retryCount: 0,
    });

    // Verify error_queue cleared
    const errorItems = await db.error_queue.toArray();
    expect(errorItems).toHaveLength(0);
  });

  it("should discard from error_queue", async () => {
    // Prepare error_queue item
    const errorItem = {
      guid: "test-guid-5",
      type: "CheckIn",
      payload: JSON.stringify({ someData: true }),
      timestamp: Date.now(),
      retryCount: 3,
      lastError: "test-error",
      failedAt: Date.now(),
    };
    await db.error_queue.add(errorItem);

    await syncService.discardFromErrorQueue("test-guid-5");

    // Verify error_queue cleared
    const errorItems = await db.error_queue.toArray();
    expect(errorItems).toHaveLength(0);
  });
});