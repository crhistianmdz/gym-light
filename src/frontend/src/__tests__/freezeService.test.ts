import fakeIndexedDB from "fake-indexeddb";
import { db } from "@/db/gymflow.db";
import { freezeMember, unfreezeMember, getFreezeHistory } from "@/services/freezeService";
import { fetchWithAuth } from "@/services/httpClient";
import { vi } from "vitest";

vi.mock("@/services/httpClient", () => ({
  fetchWithAuth: vi.fn(),
}));

const mockFetch = vi.mocked(fetchWithAuth);

describe("freezeService", () => {
  beforeAll(() => {
    global.indexedDB = fakeIndexedDB;
    vi.spyOn(db, "open").mockResolvedValue(db as any); // evitar initDatabase
  });

  afterEach(async () => {
    await db.users.clear();
  });

  it("should freeze membership and update local db on success", async () => {
    // Mock API response
    const apiResponse = { startDate: "2026-01-01", endDate: "2026-02-01" };

    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(apiResponse),
    } as Response);

    // Populate db.users
    await db.users.add({ id: "member1", fullName: "John Doe", status: "Active", membershipEndDate: "2026-06-01" });

    const result = await freezeMember("member1", { startDate: "2026-01-01" });

    // Verify API result
    expect(result).toEqual(apiResponse);

    // Verify db.users updated correctly
    const updatedUser = await db.users.get("member1");
    expect(updatedUser).toMatchObject({ status: "Frozen" });
  });

  it("should throw error when API returns server-side failure", async () => {
    // Mock API error response
    const errorResponse = { detail: "Maximum freezes reached" };

    mockFetch.mockResolvedValue({
      ok: false,
      json: () => Promise.resolve(errorResponse),
    } as Response);

    // Populate db.users
    await db.users.add({ id: "member1", fullName: "John Doe", status: "Active", membershipEndDate: "2026-06-01" });

    await expect(
      freezeMember("member1", { startDate: "2026-01-01" })
    ).rejects.toThrowError("Maximum freezes reached");

    // Verify db.users remains unchanged
    const user = await db.users.get("member1");
    expect(user).toMatchObject({ status: "Active" });
  });

  it("should unfreeze membership and update local db on success", async () => {
    // Mock API response
    const apiResponse = { status: "Active", membershipEndDate: "2026-06-01" };

    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(apiResponse),
    } as Response);

    // Populate db.users
    await db.users.add({ id: "member1", fullName: "John Doe", status: "Frozen", membershipEndDate: "2026-02-01" });

    const result = await unfreezeMember("member1");

    // Verify API result
    expect(result).toEqual(apiResponse);

    // Verify db.users updated correctly
    const updatedUser = await db.users.get("member1");
    expect(updatedUser).toMatchObject({
      status: "Active",
      membershipEndDate: "2026-06-01",
    });
  });

  it("should throw error when unfreeze API fails", async () => {
    // Mock API error response
    const errorResponse = { detail: "Member not frozen" };

    mockFetch.mockResolvedValue({
      ok: false,
      json: () => Promise.resolve(errorResponse),
    } as Response);

    // Populate db.users
    await db.users.add({ id: "member1", fullName: "John Doe", status: "Frozen", membershipEndDate: "2026-02-01" });

    await expect(unfreezeMember("member1")).rejects.toThrowError("Member not frozen");

    // Verify db.users remains unchanged
    const user = await db.users.get("member1");
    expect(user).toMatchObject({
      status: "Frozen",
      membershipEndDate: "2026-02-01",
    });
  });

  it("should retrieve freeze history successfully", async () => {
    // Mock API response
    const apiResponse = [
      { startDate: "2026-01-01", endDate: "2026-02-01" },
      { startDate: "2025-06-01", endDate: "2025-07-01" },
    ];

    mockFetch.mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(apiResponse),
    } as Response);

    const history = await getFreezeHistory("member1");

    // Verify API result
    expect(history).toHaveLength(2);
    expect(history[0]).toMatchObject({ startDate: "2026-01-01", endDate: "2026-02-01" });
  });
});