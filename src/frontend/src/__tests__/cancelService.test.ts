import fakeIndexedDB from "fake-indexeddb";
import { db } from "@/db/gymflow.db";
import { cancelMembership } from "@/services/cancelService";
import { fetchWithAuth } from "@/services/httpClient";
import { vi } from "vitest";

vi.mock("@/services/httpClient", () => ({
  fetchWithAuth: vi.fn(),
}));

vi.mock("uuid", () => ({ v4: () => "test-guid-1234" }));

const mockFetch = vi.mocked(fetchWithAuth);

describe("cancelMembership", () => {
  beforeAll(() => {
    global.indexedDB = fakeIndexedDB;
    vi.spyOn(db, "open").mockResolvedValue(db as any); // evitar initDatabase
  });

  afterEach(async () => {
    await db.sync_queue.clear();
    await db.users.clear();
  });

  it("should cancel membership and update local db when online and API returns ok", async () => {
    // Mock API response
    const apiResponse = {
      status: "Cancelled",
      autoRenewEnabled: false,
      cancelledAt: "2026-01-01",
    };

    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(apiResponse),
    } as Response);

    // Populate db.users
    await db.users.add({ id: "member1", fullName: "John Doe", status: "Active", membershipEndDate: "2026-06-01" });

    const result = await cancelMembership("member1");

    // Verify API result
    expect(result).toEqual(apiResponse);

    // Verify db.users updated correctly
    const updatedUser = await db.users.get("member1");
    expect(updatedUser).toMatchObject({
      status: "Cancelled",
      autoRenewEnabled: false,
      cancelledAt: "2026-01-01",
    });

    // Verify no sync_queue entries
    const queue = await db.sync_queue.toArray();
    expect(queue).toHaveLength(0);
  });

  it("should queue membership cancellation when offline", async () => {
    // Mock API failure
    mockFetch.mockRejectedValue(new Error("Network Error"));

    // Populate db.users
    await db.users.add({ id: "member1", fullName: "John Doe", status: "Active", membershipEndDate: "2026-06-01" });

    await expect(cancelMembership("member1")).rejects.toThrowError("OFFLINE_QUEUED");

    // Verify db.sync_queue updated correctly
    const queue = await db.sync_queue.toArray();
    expect(queue).toHaveLength(1);
    expect(queue[0]).toMatchObject({
      guid: "test-guid-1234",
      type: "MemberUpdate",
      payload: { memberId: "member1", action: "cancel", clientGuid: "test-guid-1234" },
    });

    // Verify db.users updated optimistically
    const updatedUser = await db.users.get("member1");
    expect(updatedUser).toMatchObject({
      status: "Cancelled",
      autoRenewEnabled: false,
    });
  });

  it("should throw error on API failure with server detail", async () => {
    // Mock API error response
    const errorResponse = { detail: "Member not found" };

    mockFetch.mockResolvedValue({
      ok: false,
      json: () => Promise.resolve(errorResponse),
    } as Response);

    // Populate db.users
    await db.users.add({ id: "member1", fullName: "John Doe", status: "Active", membershipEndDate: "2026-06-01" });

    await expect(cancelMembership("member1")).rejects.toThrowError("Member not found");

    // Verify db.sync_queue remains empty
    const queue = await db.sync_queue.toArray();
    expect(queue).toHaveLength(0);
  });
});