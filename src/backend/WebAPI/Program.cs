// DI REGISTRATION
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<RegisterPaymentUseCase>();
builder.Services.AddScoped<GetIncomeReportUseCase>();
builder.Services.AddScoped<GetChurnReportUseCase>();