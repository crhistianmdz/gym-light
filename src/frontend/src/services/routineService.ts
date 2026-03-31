import { db } from '../db/gymflow.db'
import type {
  ExerciseCatalogItem,
  Routine,
  RoutineAssignment,
  WorkoutLog,
  CreateRoutineRequest,
  CreateWorkoutLogRequest
} from '../types/routine'

const API_BASE = import.meta.env.VITE_API_URL ?? '/api'

function getAuthHeader(): Record<string, string> {
  const token = sessionStorage.getItem('access_token')
  return token ? { Authorization: `Bearer ${token}` } : {}
}

// --- Exercise Catalog ---

export async function getExerciseCatalog(nameFilter?: string): Promise<ExerciseCatalogItem[]> {
  try {
    const url = nameFilter
      ? `${API_BASE}/exercise-catalog?name=${encodeURIComponent(nameFilter)}`
      : `${API_BASE}/exercise-catalog`
    const res = await fetch(url, { headers: getAuthHeader() })
    if (!res.ok) throw new Error('API error')
    const data: ExerciseCatalogItem[] = await res.json()
    // Cachear en IndexedDB
    await db.table('exercise_catalog').bulkPut(data)
    return data
  } catch {
    // Fallback offline
    const items = await db.table('exercise_catalog').toArray()
    if (nameFilter) {
      return items.filter((e: ExerciseCatalogItem) =>
        e.name.toLowerCase().includes(nameFilter.toLowerCase()))
    }
    return items
  }
}

export async function createExercise(data: Omit<ExerciseCatalogItem, 'id'>): Promise<ExerciseCatalogItem> {
  const res = await fetch(`${API_BASE}/exercise-catalog`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...getAuthHeader() },
    body: JSON.stringify(data)
  })
  if (!res.ok) throw new Error('Error al crear ejercicio')
  return res.json()
}

// --- Routines ---

export async function getRoutines(params?: { isPublic?: boolean; mine?: boolean }): Promise<Routine[]> {
  try {
    const qs = new URLSearchParams()
    if (params?.isPublic !== undefined) qs.set('isPublic', String(params.isPublic))
    if (params?.mine) qs.set('mine', 'true')
    const res = await fetch(`${API_BASE}/routines?${qs}`, { headers: getAuthHeader() })
    if (!res.ok) throw new Error('API error')
    const data: Routine[] = await res.json()
    await db.table('routines').bulkPut(data)
    return data
  } catch {
    return db.table('routines').toArray()
  }
}

export async function createRoutine(data: CreateRoutineRequest): Promise<Routine> {
  const res = await fetch(`${API_BASE}/routines`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...getAuthHeader() },
    body: JSON.stringify(data)
  })
  if (!res.ok) throw new Error('Error al crear rutina')
  return res.json()
}

export async function updateRoutine(id: string, data: Omit<CreateRoutineRequest, 'createdByUserId'>): Promise<Routine> {
  const res = await fetch(`${API_BASE}/routines/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...getAuthHeader() },
    body: JSON.stringify(data)
  })
  if (!res.ok) throw new Error('Error al actualizar rutina')
  return res.json()
}

export async function assignRoutine(routineId: string, memberId: string): Promise<RoutineAssignment> {
  const res = await fetch(`${API_BASE}/routine-assignments`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...getAuthHeader() },
    body: JSON.stringify({ routineId, memberId })
  })
  if (!res.ok) throw new Error('Error al asignar rutina')
  return res.json()
}

export async function getMemberRoutines(memberId: string): Promise<RoutineAssignment[]> {
  try {
    const res = await fetch(`${API_BASE}/members/${memberId}/routines`, { headers: getAuthHeader() })
    if (!res.ok) throw new Error('API error')
    const data: RoutineAssignment[] = await res.json()
    await db.table('routine_assignments').bulkPut(data)
    return data
  } catch {
    return db.table('routine_assignments')
      .where('memberId').equals(memberId).toArray()
  }
}

// --- Workout Logs ---

export async function createWorkoutLog(data: CreateWorkoutLogRequest): Promise<WorkoutLog> {
  try {
    const res = await fetch(`${API_BASE}/workout-logs`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', ...getAuthHeader() },
      body: JSON.stringify(data)
    })
    if (!res.ok) throw new Error('API error')
    const result = await res.json()
    const log = result.alreadyProcessed ? result.data : result
    await db.table('workout_logs').put({ ...log, syncStatus: 'synced' })
    return log
  } catch {
    // Offline: guardar en IndexedDB y encolar en sync_queue
    const offlineLog: WorkoutLog & { syncStatus: string } = {
      id: crypto.randomUUID(),
      ...data,
      sessionDate: data.sessionDate,
      memberId: '',   // se completará en sync
      createdAt: new Date().toISOString(),
      entries: data.entries.map(e => ({
        routineExerciseId: e.routineExerciseId,
        exerciseName: '',
        sets: 0,
        reps: 0,
        completed: e.completed,
        completedAt: e.completed ? new Date().toISOString() : undefined,
        notes: e.notes
      })),
      syncStatus: 'pending'
    }
    await db.table('workout_logs').put(offlineLog)
    await db.table('sync_queue').put({
      guid: data.clientGuid,
      type: 'WorkoutLogCreate',
      payload: JSON.stringify(data),
      timestamp: Date.now()
    })
    return offlineLog
  }
}

export async function getMemberWorkoutLogs(memberId: string): Promise<WorkoutLog[]> {
  try {
    const res = await fetch(`${API_BASE}/members/${memberId}/workout-logs`, { headers: getAuthHeader() })
    if (!res.ok) throw new Error('API error')
    const data: WorkoutLog[] = await res.json()
    await db.table('workout_logs').bulkPut(data.map(l => ({ ...l, syncStatus: 'synced' })))
    return data
  } catch {
    return db.table('workout_logs').where('memberId').equals(memberId).toArray()
  }
}