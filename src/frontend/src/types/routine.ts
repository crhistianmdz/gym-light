export interface ExerciseCatalogItem {
  id: string;
  name: string;
  description?: string;
  mediaUrl?: string;
  isCustom: boolean;
}

export interface RoutineExercise {
  id?: string;
  exerciseCatalogId?: string;
  customName?: string;
  order: number;
  sets: number;
  reps: number;
  notes?: string;
}

export interface Routine {
  id: string;
  name: string;
  description?: string;
  isPublic: boolean;
  createdByUserId: string;
  createdAt: string;
  updatedAt: string;
  exercises: RoutineExercise[];
}

export interface RoutineAssignment {
  id: string;
  routineId: string;
  routineName: string;
  memberId: string;
  assignedByUserId: string;
  assignedAt: string;
  isActive: boolean;
}

export interface WorkoutEntry {
  routineExerciseId: string;
  exerciseName: string;
  sets: number;
  reps: number;
  completed: boolean;
  completedAt?: string;
  notes?: string;
}

export interface WorkoutLog {
  id: string;
  assignmentId: string;
  memberId: string;
  sessionDate: string;
  clientGuid: string;
  createdAt: string;
  entries: WorkoutEntry[];
}

export interface CreateRoutineExerciseRequest {
  exerciseCatalogId?: string;
  customName?: string;
  order: number;
  sets: number;
  reps: number;
  notes?: string;
}

export interface CreateRoutineRequest {
  name: string;
  description?: string;
  isPublic: boolean;
  exercises: CreateRoutineExerciseRequest[];
}

export interface CreateWorkoutLogRequest {
  assignmentId: string;
  sessionDate: string;
  clientGuid: string;
  entries: Array<{
    routineExerciseId: string;
    completed: boolean;
    notes?: string;
  }>;
}