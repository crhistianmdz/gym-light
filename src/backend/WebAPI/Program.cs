// HU-11: Rutinas Digitales
builder.Services.AddScoped<CreateExerciseUseCase>();
builder.Services.AddScoped<GetExercisesUseCase>();
builder.Services.AddScoped<CreateRoutineUseCase>();
builder.Services.AddScoped<UpdateRoutineUseCase>();
builder.Services.AddScoped<GetRoutinesUseCase>();
builder.Services.AddScoped<AssignRoutineUseCase>();
builder.Services.AddScoped<GetMemberRoutinesUseCase>();
builder.Services.AddScoped<CreateWorkoutLogUseCase>();
builder.Services.AddScoped<GetWorkoutLogsUseCase>();