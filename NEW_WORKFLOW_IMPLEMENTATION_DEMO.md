# ?? GUÍA DE IMPLEMENTACIÓN: NUEVO WORKFLOW DESDE CERO

**Propósito**: Demostración en vivo para equipos técnicos  
**Nivel**: Intermedio  
**Tiempo estimado**: 30-45 minutos  
**Fecha**: $(Get-Date -Format "yyyy-MM-dd")

---

## ?? TABLA DE CONTENIDOS

1. [Introducción](#introducción)
2. [Caso de Uso: Workflow de Préstamo Personal](#caso-de-uso)
3. [Paso 1: Definir Campos Personalizados](#paso-1-campos)
4. [Paso 2: Configurar el Workflow](#paso-2-workflow)
5. [Paso 3: Implementar Validación Personalizada](#paso-3-validación)
6. [Paso 4: Probar el Flujo Completo](#paso-4-pruebas)
7. [Checklist de Implementación](#checklist)
8. [Troubleshooting](#troubleshooting)

---

## ?? INTRODUCCIÓN

Esta guía demuestra cómo implementar un **workflow completamente nuevo** en el sistema de Onboarding, incluyendo:

- ? Creación de **campos personalizados** con validaciones
- ? Configuración de **fases y pasos** del workflow
- ? Implementación de **validaciones personalizadas** usando Validation Providers
- ? Pruebas end-to-end del flujo completo

### **Arquitectura del Sistema**

```
???????????????????????????????????????????????????????????????????
?                      NUEVO WORKFLOW                             ?
???????????????????????????????????????????????????????????????????
?                                                                 ?
?  Workflow 30: Préstamo Personal                                ?
?  ?? Fase 1: Datos Personales                                   ?
?  ?  ?? Step 1: Información Básica                              ?
?  ?      ?? Campo: monthly_income (NUMBER + Validación)         ?
?  ?      ?? Campo: employment_status (TEXT + Valores Permitidos)?
?  ?                                                              ?
?  ?? Fase 2: Validación Crediticia                              ?
?     ?? Step 2: Verificación                                    ?
?         ?? Validación: MinimumIncomeValidator (Provider)       ?
?                                                                 ?
???????????????????????????????????????????????????????????????????
```

---

## ?? CASO DE USO: WORKFLOW DE PRÉSTAMO PERSONAL

### **Descripción del Negocio**

**Workflow ID**: 30  
**Nombre**: Préstamo Personal  
**Objetivo**: Capturar información financiera y validar elegibilidad para préstamos

### **Requerimientos del Negocio**

1. **Capturar información del solicitante**:
   - Ingreso mensual (obligatorio, mínimo $5,000 MXN)
   - Estatus de empleo (Empleado, Freelance, Empresario)

2. **Validar elegibilidad**:
   - El ingreso mensual debe ser al menos $10,000 MXN
   - El estatus de empleo debe ser "Empleado" o "Empresario"

3. **Flujo de 2 pasos**:
   - Paso 1: Captura de datos
   - Paso 2: Validación automática

---

## ?? PASO 1: DEFINIR CAMPOS PERSONALIZADOS

### **1.1. Campos Necesarios**

Vamos a crear **2 campos nuevos**:

| Campo             | Tipo   | Scope       | Validaciones                          |
|-------------------|--------|-------------|---------------------------------------|
| `monthly_income`  | NUMBER | APPLICATION | min_value: 5000, is_required: true    |
| `employment_status` | TEXT | APPLICATION | allowed_values, is_required: true     |

---

### **1.2. Código SQL para Insertar Campos**

**Archivo**: `OvexDataModelingTest\Data\OvexDataSeeder.cs`

Agrega el siguiente código en el método `Seed`, en la sección de **FieldDefinitions**:

```csharp
// ============================================
// WORKFLOW 30: PRÉSTAMO PERSONAL - CAMPOS
// ============================================

var fMonthlyIncome = new FieldDefinition
{
    FieldId = 20,
    FieldKey = "monthly_income",
    DataType = "NUMBER",
    Scope = "APPLICATION",
    Config = JsonSerializer.Serialize(new
    {
        is_required = true,
        min_value = 5000,
        label = "Ingreso Mensual",
        placeholder = "Ej: 15000",
        help_text = "Ingreso mensual neto en MXN (mínimo $5,000)"
    })
};

var fEmploymentStatus = new FieldDefinition
{
    FieldId = 21,
    FieldKey = "employment_status",
    DataType = "TEXT",
    Scope = "APPLICATION",
    Config = JsonSerializer.Serialize(new
    {
        is_required = true,
        allowed_values = new[] { "Empleado", "Freelance", "Empresario", "Desempleado" },
        label = "Estatus de Empleo",
        help_text = "Selecciona tu situación laboral actual"
    })
};

db.FieldDefinitions.AddRange(fMonthlyIncome, fEmploymentStatus);
```

---

### **1.3. ¿Qué hace este código?**

- **`FieldId`**: Identificador único del campo (asegúrate de que no colisione con otros)
- **`FieldKey`**: Nombre técnico del campo (snake_case)
- **`DataType`**: Tipo de dato (`NUMBER`, `TEXT`, `DATE`, etc.)
- **`Scope`**: 
  - `USER`: Datos del usuario (ej: email, nombre)
  - `APPLICATION`: Datos de la solicitud (ej: ingreso, monto)
- **`Config`**: JSON con configuración de validación y UI

---

## ?? PASO 2: CONFIGURAR EL WORKFLOW

### **2.1. Crear el Workflow Principal**

Agrega el siguiente código en `OvexDataSeeder.cs`:

```csharp
// ============================================
// WORKFLOW 30: PRÉSTAMO PERSONAL
// ============================================

var workflow30 = new Workflow
{
    WorkflowId = 30,
    Name = "Préstamo Personal",
    Description = "Solicitud de préstamo personal con validación de ingreso",
    IsActive = true
};
db.Workflows.Add(workflow30);

// REGLA DE ENRUTAMIENTO
var rule30 = new RoutingRule
{
    RuleId = 30,
    WorkflowId = 30,
    Priority = 30,
    Condition = JsonSerializer.Serialize(new
    {
        product_type = "personal_loan"
    }),
    IsActive = true
};
db.RoutingRules.Add(rule30);
```

---

### **2.2. Crear Fases y Pasos**

```csharp
// FASE 1: Datos Personales
var phase30_1 = new Phase 
{ 
    PhaseId = 20, 
    WorkflowId = 30, 
    Name = "Datos Personales", 
    Order = 1 
};
db.Phases.Add(phase30_1);

var step30_1 = new Step 
{ 
    StepId = 20, 
    PhaseId = 20, 
    Name = "Información Financiera", 
    Order = 1 
};
db.Steps.Add(step30_1);

// VINCULAR CAMPOS AL PASO
db.StepFields.AddRange(
    new Step_Field 
    { 
        StepId = 20, 
        FieldId = 20, // monthly_income
        ConfigOverride = null 
    },
    new Step_Field 
    { 
        StepId = 20, 
        FieldId = 21, // employment_status
        ConfigOverride = null 
    }
);

// FASE 2: Validación Crediticia
var phase30_2 = new Phase 
{ 
    PhaseId = 21, 
    WorkflowId = 30, 
    Name = "Validación Crediticia", 
    Order = 2 
};
db.Phases.Add(phase30_2);

var step30_2 = new Step 
{ 
    StepId = 21, 
    PhaseId = 21, 
    Name = "Verificación de Elegibilidad", 
    Order = 1 
};
db.Steps.Add(step30_2);
```

---

### **2.3. Diagrama del Workflow**

```
Workflow 30: Préstamo Personal
?
?? Fase 1: Datos Personales
?  ?? Step 20: Información Financiera
?      ?? monthly_income (NUMBER, required, min: 5000)
?      ?? employment_status (TEXT, required, enum)
?
?? Fase 2: Validación Crediticia
   ?? Step 21: Verificación de Elegibilidad
       ?? Validación: MinimumIncomeValidator
```

---

## ? PASO 3: IMPLEMENTAR VALIDACIÓN PERSONALIZADA

### **3.1. Crear Validation Provider**

**Archivo**: `Onboarding.Infrastructure\Validation\Providers\MinimumIncomeValidator.cs`

```csharp
using Microsoft.Extensions.Logging;
using Onboarding.Core.Validation;
using System.Text.Json;

namespace Onboarding.Infrastructure.Validation.Providers
{
    /// <summary>
    /// Valida que el ingreso mensual cumpla con el mínimo requerido para préstamos.
    /// </summary>
    public class MinimumIncomeValidator : IValidationProvider
    {
        private readonly ILogger<MinimumIncomeValidator> _logger;

        public string ProviderKey => "MINIMUM_INCOME";

        public MinimumIncomeValidator(ILogger<MinimumIncomeValidator> logger)
        {
            _logger = logger;
        }

        public async Task<ValidationStepResult> ValidateAsync(ValidationContext context)
        {
            var result = new ValidationStepResult
            {
                ProviderKey = ProviderKey,
                IsValid = true
            };

            try
            {
                // 1. Parsear el payload para obtener monthly_income
                var payloadJson = JsonDocument.Parse(context.PayloadJson);
                var monthlyIncomeElement = payloadJson.RootElement.GetProperty("monthly_income");
                var monthlyIncome = monthlyIncomeElement.GetDecimal();

                _logger.LogInformation("Validating income: {Income} for ProspectId: {ProspectId}", 
                    monthlyIncome, context.ProspectId);

                // 2. Obtener el mínimo requerido de la configuración
                var configJson = JsonDocument.Parse(context.ConfigJson ?? "{}");
                var minimumRequired = configJson.RootElement.TryGetProperty("minimum_income", out var minElement)
                    ? minElement.GetDecimal()
                    : 10000m; // Default: $10,000 MXN

                // 3. Validar
                if (monthlyIncome < minimumRequired)
                {
                    result.IsValid = false;
                    result.ErrorCode = "INSUFFICIENT_INCOME";
                    result.Message = $"El ingreso mensual (${monthlyIncome:N2}) no cumple con el mínimo requerido (${minimumRequired:N2})";
                    
                    _logger.LogWarning("Income validation failed: {Income} < {Required}", 
                        monthlyIncome, minimumRequired);
                }
                else
                {
                    result.Message = $"Ingreso validado correctamente: ${monthlyIncome:N2}";
                    _logger.LogInformation("Income validation passed: {Income}", monthlyIncome);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating minimum income");
                
                result.IsValid = false;
                result.ErrorCode = "VALIDATION_ERROR";
                result.Message = "Error al validar el ingreso mensual";
                
                return result;
            }
        }
    }
}
```

---

### **3.2. Registrar el Provider en DI**

**Archivo**: `Onboarding.Infrastructure\Extensions\ServiceCollectionExtensions.cs`

Busca la sección de **Validation Providers** y agrega:

```csharp
// 10. VALIDATION PROVIDERS
services.AddScoped<IValidationProvider, DuplicateEmailValidator>();
services.AddScoped<IValidationProvider, RuleEngineValidationProvider>();
services.AddScoped<IValidationProvider, MinimumIncomeValidator>(); // ? NUEVO
```

---

### **3.3. Configurar el Pipeline de Validación**

Agrega en `OvexDataSeeder.cs`:

```csharp
// ============================================
// VALIDATION PROVIDERS: PRÉSTAMO PERSONAL
// ============================================

// Registrar el provider en la base de datos
db.ValidationProviders.Add(new ValidationProvider
{
    ProviderId = 2,
    ProviderKey = "MINIMUM_INCOME",
    ProviderType = "INTERNAL_CODE",
    ConfigJson = JsonSerializer.Serialize(new 
    { 
        minimum_income = 10000,
        description = "Valida que el ingreso mensual cumpla con el mínimo requerido"
    }),
    IsActive = true
});

// ============================================
// VALIDATION PIPELINE: PRÉSTAMO PERSONAL
// ============================================

var pipeline30 = new ValidationPipeline
{
    PipelineId = 2,
    PipelineKey = "PERSONAL_LOAN_VALIDATION",
    Name = "Pipeline de Validación para Préstamo Personal",
    TriggerContext = "StepDataSubmitted",
    WorkflowId = 30,
    StepId = 21, // Validar en el paso 2
    Priority = 10,
    IsActive = true,
    StopOnFirstFailure = true,
    RequireAllPass = true
};
db.ValidationPipelines.Add(pipeline30);

var pipelineStep30 = new PipelineStep
{
    PipelineStepId = 2,
    PipelineId = 2,
    ProviderId = 2, // ID del provider "MINIMUM_INCOME"
    ExecutionOrder = 1,
    IsActive = true,
    OnFailureAction = "STOP",
    ConfigOverrideJson = JsonSerializer.Serialize(new
    {
        minimum_income = 10000 // Mínimo $10,000 MXN
    })
};
db.PipelineSteps.Add(pipelineStep30);
```

---

### **3.4. ¿Qué hace este código?**

- **`ValidationProvider`**: Registra el provider en la base de datos
  - `ProviderKey`: Debe coincidir con la propiedad `ProviderKey` de la clase C#
  - `ProviderType`: `INTERNAL_CODE` para providers implementados en C#
  - `ConfigJson`: Configuración por defecto del provider

- **`ValidationPipeline`**: Define cuándo se ejecuta el pipeline
  - `TriggerContext`: `StepDataSubmitted` = se ejecuta al enviar datos de un step
  - `WorkflowId` y `StepId`: Define en qué workflow y paso se ejecuta

- **`PipelineStep`**: Vincula el provider al pipeline
  - `ProviderId`: Referencia al provider registrado
  - `ExecutionOrder`: Orden de ejecución si hay múltiples providers
  - `ConfigOverrideJson`: Puede sobrescribir la configuración del provider

---

## ?? PASO 3.5: CREAR PÁGINA RAZOR PARA EL NUEVO WORKFLOW

### **3.5.1. Crear Página de Inicio**

**Archivo**: `Onboarding.TestClient\Pages\Start\PersonalLoan.cshtml`

```razor
@page
@model Onboarding.TestClient.Pages.Start.PersonalLoanModel
@{
    ViewData["Title"] = "Solicitud de Préstamo Personal";
}

<div class="container py-5">
    <div class="row justify-content-center">
        <div class="col-md-8 col-lg-6">
            <div class="card shadow-lg border-0">
                <div class="card-body p-5">
                    <!-- Header -->
                    <div class="text-center mb-4">
                        <i class="bi bi-cash-coin display-3 text-success"></i>
                        <h1 class="h3 fw-bold mt-3">Préstamo Personal</h1>
                        <p class="text-muted">
                            Solicita hasta $5,000,000 MXN con tasas competitivas
                        </p>
                    </div>

                    <!-- Error Message -->
                    @if (!string.IsNullOrEmpty(Model.ErrorMessage))
                    {
                        <div class="alert alert-danger alert-dismissible fade show" role="alert">
                            <i class="bi bi-exclamation-triangle me-2"></i>
                            @Model.ErrorMessage
                            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
                        </div>
                    }

                    <!-- Form -->
                    <form method="post" asp-page-handler="Start">
                        <div class="mb-3">
                            <label for="Email" class="form-label">
                                <i class="bi bi-envelope me-2"></i>Correo Electrónico
                            </label>
                            <input type="email" 
                                   class="form-control form-control-lg" 
                                   id="Email" 
                                   name="Email" 
                                   placeholder="tu@email.com" 
                                   required 
                                   value="@Model.Email">
                            <div class="form-text">
                                Usaremos este correo para enviarte actualizaciones
                            </div>
                        </div>

                        <div class="mb-4">
                            <label for="MonthlyIncome" class="form-label">
                                <i class="bi bi-wallet2 me-2"></i>Ingreso Mensual Estimado
                            </label>
                            <div class="input-group input-group-lg">
                                <span class="input-group-text">$</span>
                                <input type="number" 
                                       class="form-control" 
                                       id="MonthlyIncome" 
                                       name="MonthlyIncome" 
                                       placeholder="15000" 
                                       min="5000"
                                       required 
                                       value="@Model.MonthlyIncome">
                                <span class="input-group-text">MXN</span>
                            </div>
                            <div class="form-text">
                                Mínimo: $5,000 MXN
                            </div>
                        </div>

                        <!-- Info Box -->
                        <div class="alert alert-info d-flex align-items-center mb-4" role="alert">
                            <i class="bi bi-info-circle fs-4 me-3"></i>
                            <div>
                                <strong>Proceso rápido y seguro</strong><br>
                                <small>Completa 2 pasos simples y obtén respuesta en minutos</small>
                            </div>
                        </div>

                        <!-- Submit Button -->
                        <button type="submit" class="btn btn-success btn-lg w-100">
                            <i class="bi bi-arrow-right-circle me-2"></i>
                            Iniciar Solicitud
                        </button>
                    </form>

                    <!-- Benefits -->
                    <div class="mt-4">
                        <h6 class="text-muted mb-3">Beneficios:</h6>
                        <ul class="list-unstyled">
                            <li class="mb-2">
                                <i class="bi bi-check-circle-fill text-success me-2"></i>
                                Sin comisión por apertura
                            </li>
                            <li class="mb-2">
                                <i class="bi bi-check-circle-fill text-success me-2"></i>
                                Respuesta en 24 horas
                            </li>
                            <li class="mb-2">
                                <i class="bi bi-check-circle-fill text-success me-2"></i>
                                Tasas desde 12% anual
                            </li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

<style>
    .card {
        border-radius: 15px;
    }
    .btn-success {
        border-radius: 10px;
        padding: 15px;
        font-weight: 600;
    }
</style>
```

---

### **3.5.2. Crear Code-Behind**

**Archivo**: `Onboarding.TestClient\Pages\Start\PersonalLoan.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Onboarding.TestClient.Models;
using Onboarding.TestClient.Services;
using System.Text.Json;

namespace Onboarding.TestClient.Pages.Start
{
    public class PersonalLoanModel : PageModel
    {
        private readonly IOnboardingApiClient _apiClient;
        private readonly ILogger<PersonalLoanModel> _logger;

        public PersonalLoanModel(
            IOnboardingApiClient apiClient,
            ILogger<PersonalLoanModel> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public decimal MonthlyIncome { get; set; }

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            // Página de inicio - no hace nada
        }

        public async Task<IActionResult> OnPostStartAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Por favor, completa todos los campos correctamente.";
                return Page();
            }

            try
            {
                _logger.LogInformation("Iniciando solicitud de préstamo personal para {Email}", Email);

                // 1. Crear el prospecto
                var createRequest = new CreateProspectRequest
                {
                    Email = Email,
                    WorkflowId = 30, // ID del workflow de Préstamo Personal
                    PayloadJson = JsonSerializer.Serialize(new
                    {
                        email = Email,
                        product_type = "personal_loan",
                        monthly_income = MonthlyIncome
                    })
                };

                var response = await _apiClient.CreateProspectAsync(createRequest);

                if (response == null)
                {
                    ErrorMessage = "Error al crear la solicitud. Por favor, intenta nuevamente.";
                    _logger.LogError("Failed to create prospect for {Email}", Email);
                    return Page();
                }

                // 2. Guardar datos en TempData y Cookie para el wizard
                TempData["ProspectId"] = response.ProspectId.ToString();
                TempData["WorkflowId"] = 30;
                TempData["OnboardingType"] = "Préstamo Personal";
                TempData["UserEmail"] = Email;

                Response.Cookies.Append("ProspectId", response.ProspectId.ToString());
                Response.Cookies.Append("WorkflowId", "30");

                _logger.LogInformation("Prospect created successfully: {ProspectId}", response.ProspectId);

                // 3. Redirigir al wizard
                return RedirectToPage("/Start/Wizard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating personal loan prospect");
                ErrorMessage = "Ocurrió un error inesperado. Por favor, intenta nuevamente.";
                return Page();
            }
        }
    }
}
```

---

### **3.5.3. Agregar Botón en la Página Principal**

**Archivo**: `Onboarding.TestClient\Pages\Index.cshtml`

Busca la sección de tarjetas de workflows y agrega:

```razor
<!-- Tarjetas de Workflows Existentes -->
<!-- ... OVEX Cliente Individual ... -->
<!-- ... OVEX Flotilla ... -->
<!-- ... PCH Persona Moral ... -->

<!-- ? NUEVA TARJETA: Préstamo Personal -->
<div class="col-md-6 col-lg-4 mb-4">
    <div class="card h-100 shadow-sm workflow-card" data-workflow="personal-loan">
        <div class="card-body text-center p-4">
            <div class="workflow-icon mb-3">
                <i class="bi bi-cash-coin display-3 text-success"></i>
            </div>
            <h5 class="card-title fw-bold">Préstamo Personal</h5>
            <p class="card-text text-muted">
                Solicita un préstamo personal con aprobación rápida y tasas competitivas
            </p>
            <ul class="list-unstyled text-start mb-4">
                <li class="mb-2">
                    <i class="bi bi-check-circle text-success me-2"></i>
                    Hasta $5,000,000 MXN
                </li>
                <li class="mb-2">
                    <i class="bi bi-check-circle text-success me-2"></i>
                    Sin comisión por apertura
                </li>
                <li class="mb-2">
                    <i class="bi bi-check-circle text-success me-2"></i>
                    Respuesta en 24 horas
                </li>
            </ul>
            <a href="/Start/PersonalLoan" class="btn btn-success w-100">
                <i class="bi bi-arrow-right-circle me-2"></i>
                Solicitar Ahora
            </a>
        </div>
        <div class="card-footer bg-transparent border-top-0">
            <small class="text-muted">
                <i class="bi bi-clock me-1"></i>
                2 pasos • ~5 minutos
            </small>
        </div>
    </div>
</div>
```

---

### **3.5.4. Estilos Adicionales (Opcional)**

Agrega al final de `Index.cshtml`:

```razor
@section Styles {
    <style>
        .workflow-card {
            transition: all 0.3s ease;
            border: none;
            border-radius: 15px;
        }

        .workflow-card:hover {
            transform: translateY(-10px);
            box-shadow: 0 1rem 3rem rgba(0, 0, 0, 0.175) !important;
        }

        .workflow-card[data-workflow="personal-loan"] {
            border-left: 5px solid #198754;
        }

        .workflow-icon {
            animation: pulse 2s ease-in-out infinite;
        }

        @keyframes pulse {
            0%, 100% {
                transform: scale(1);
            }
            50% {
                transform: scale(1.05);
            }
        }
    </style>
}

```

## ? CHECKLIST DE IMPLEMENTACIÓN

### **Fase 1: Configuración (Base de Datos)**

- [ ] **Campos definidos** en `FieldDefinition` con IDs únicos
- [ ] **Workflow creado** con `WorkflowId` único
- [ ] **RoutingRule configurada** con condiciones claras
- [ ] **Fases y Steps** creados con orden correcto
- [ ] **Step_Field** vincula campos a pasos
- [ ] **ValidationProvider registrado** con `ProviderKey` correcto ? NUEVO
- [ ] **ValidationPipeline** configurado con trigger correcto
- [ ] **PipelineStep** vincula provider a pipeline ? NUEVO

### **Fase 2: Código (Backend)**

- [ ] **Validation Provider** implementa `IValidationProvider`
- [ ] **Provider registrado** en `ServiceCollectionExtensions.cs`
- [ ] **Logs informativos** en el provider
- [ ] **Manejo de excepciones** completo

### **Fase 3: Frontend (Páginas Razor)** ? NUEVO

- [ ] **Página de inicio** creada (`PersonalLoan.cshtml`)
- [ ] **Code-behind** implementado (`PersonalLoan.cshtml.cs`)
- [ ] **Botón agregado** en `Index.cshtml` con estilos
- [ ] **Redirección correcta** al wizard después de crear prospecto
- [ ] **TempData y Cookies** configurados correctamente

### **Fase 4: Pruebas**

- [ ] **Seeder ejecutado** sin errores
- [ ] **Workflow visible** en dashboard
- [ ] **Página de inicio** accesible desde Index
- [ ] **Creación de prospecto** funciona correctamente
- [ ] **Wizard carga** los campos del workflow 30
- [ ] **Validación exitosa** con datos correctos
- [ ] **Validación fallida** con datos incorrectos
- [ ] **Logs del provider** visibles en consola

---

### **Problema 4: Campos No Se Muestran en el Wizard**

**Síntoma**: Los campos no aparecen en el formulario

**Soluciones**:
1. Verificar que `Step_Field` vincula correctamente `StepId` y `FieldId`
2. Verificar que el `DataType` es reconocido por el wizard (`TEXT`, `NUMBER`, etc.)
3. Limpiar cache del navegador

---

### **Problema 5: Página de Inicio No Funciona**

**Síntoma**: Error 404 al acceder a `/Start/PersonalLoan`

**Soluciones**:
1. Verificar que los archivos están en la carpeta correcta:
   - `Onboarding.TestClient\Pages\Start\PersonalLoan.cshtml`
   - `Onboarding.TestClient\Pages\Start\PersonalLoan.cshtml.cs`
2. Verificar que el namespace es correcto: `Onboarding.TestClient.Pages.Start`
3. Limpiar y recompilar el proyecto:
   ```powershell
   dotnet clean
   dotnet build
   ```
4. Reiniciar el servidor de desarrollo

---

### **Problema 6: ValidationProvider No Aparece en BD**

**Síntoma**: El provider no se encuentra al ejecutar el pipeline

**Soluciones**:
1. Verificar que el `ProviderId` es único y no colisiona
2. Verificar que `ProviderKey` coincide exactamente con la clase C#:
   - **Seeder**: `ProviderKey = "MINIMUM_INCOME"`
   - **Clase**: `public string ProviderKey => "MINIMUM_INCOME";`
3. Ejecutar el seeder nuevamente (borrar BD y reiniciar API)
4. Verificar logs: `"Registered provider: MINIMUM_INCOME"`

---

## ?? PUNTOS CLAVE PARA LA DEMO

### **Lo que el equipo debe entender**:

1. **Configuración Data-Driven**:
   - Todo está en la BD (no hay código hardcoded)
   - Cambios en configuración NO requieren recompilar

2. **Extensibilidad**:
   - Agregar un campo nuevo: Solo insertar en `FieldDefinition`
   - Agregar una validación: Crear un provider e insertar en `ValidationPipeline`
   - Agregar un workflow: Crear página Razor + configuración en seeder

3. **Separación de Responsabilidades**:
   - **Core**: Lógica de negocio (Validators)
   - **Infrastructure**: Implementaciones concretas (Providers)
   - **Data**: Configuración (Seeder)
   - **TestClient**: UI (Razor Pages)

4. **Trazabilidad**:
   - Cada validación tiene logs
   - Cada cambio de estado tiene historial
   - Cada error tiene código y mensaje descriptivo

5. **Flujo Completo**:
   - Usuario entra a `/Start/PersonalLoan`
   - Completa email e ingreso estimado
   - Se crea `Prospect` con `WorkflowId = 30`
   - Redirige al `Wizard` dinámico
   - Wizard carga campos desde `Step_Field`
   - Usuario completa paso 1 (datos financieros)
   - `StepDataSubmitted` event dispara `ValidationPipeline`
   - `MinimumIncomeValidator` valida ingreso >= $10,000
   - Si pasa: prospecto aprobado, si falla: mensaje de error descriptivo
