using OvexDataModelingTest.Entities.App;
using OvexDataModelingTest.Entities.Config;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace OvexDataModelingTest.Data
{
    public static class OvexDataSeeder
    {
        public static void Seed(OnboardingDbContext db)
        {
            if (db.Workflows.Any()) return;

            Console.WriteLine("--> [SEEDER] Inicializando Configuración de Workflows...");

            // ==========================================
            // 1. WORKFLOWS
            // ==========================================
            var wfOvexClient = new Workflow 
            { 
                WorkflowId = 1, 
                WorkflowType = "PROSPECT", 
                SubTypeKey = "OVEX_CLIENT", 
                Name = "OVEX - Cliente Individual", 
                IsActive = true 
            };
            
            var wfOvexFleet = new Workflow 
            { 
                WorkflowId = 2, 
                WorkflowType = "PROSPECT", 
                SubTypeKey = "OVEX_FLOTILLA", 
                Name = "OVEX - Flotilla Empresarial", 
                IsActive = true 
            };
            
            var wfPchPM = new Workflow 
            { 
                WorkflowId = 3, 
                WorkflowType = "PROSPECT", 
                SubTypeKey = "PCH_PM", 
                Name = "PCH Capital - Persona Moral", 
                IsActive = true 
            };

            db.Workflows.AddRange(wfOvexClient, wfOvexFleet, wfPchPM);

            // ==========================================
            // 2. RUTEO INICIAL (RoutingRules)
            // ==========================================
            db.WorkflowRoutingRules.AddRange(
                new WorkflowRoutingRule
                {
                    RoutingRuleId = 1,
                    Priority = 1,
                    ConditionExpression = "app_id == \"OVEX\" && client_type == \"CLIENT\"",
                    TargetWorkflowId = 1
                },
                new WorkflowRoutingRule
                {
                    RoutingRuleId = 2,
                    Priority = 2,
                    ConditionExpression = "app_id == \"OVEX\" && client_type == \"FLOTILLA_EMP\"",
                    TargetWorkflowId = 2
                },
                new WorkflowRoutingRule
                {
                    RoutingRuleId = 3,
                    Priority = 3,
                    ConditionExpression = "app_id == \"PCH\" && client_type == \"PM\"",
                    TargetWorkflowId = 3
                }
            );

            // ==========================================
            // 3. CATALOGO DE CAMPOS (FieldDefinitions)
            // ==========================================
            var fFullName = new FieldDefinition 
            { 
                FieldId = 1, 
                FieldKey = "full_name", 
                DataType = "TEXT", 
                Scope = "USER", 
                Config = JsonSerializer.Serialize(new 
                { 
                    is_required = true,
                    min_length = 3,
                    max_length = 100,
                    label = "Nombre Completo",
                    placeholder = "Ingresa tu nombre completo",
                    help_text = "Nombre y apellidos como aparecen en tu identificación oficial"
                })
            };
            
            var fEmail = new FieldDefinition 
            { 
                FieldId = 2, 
                FieldKey = "email", 
                DataType = "EMAIL", 
                Scope = "USER", 
                Config = JsonSerializer.Serialize(new 
                { 
                    is_required = true,
                    label = "Correo Electrónico",
                    placeholder = "tu@email.com",
                    help_text = "Usaremos este correo para comunicarnos contigo"
                })
            };

            var fPhone = new FieldDefinition
            {
                FieldId = 3,
                FieldKey = "phone",
                DataType = "PHONE",
                Scope = "USER",
                Config = JsonSerializer.Serialize(new
                {
                    is_required = true,
                    min_length = 10,
                    max_length = 15,
                    label = "Teléfono",
                    placeholder = "5512345678",
                    help_text = "Teléfono celular a 10 dígitos"
                })
            };

            var fCurp = new FieldDefinition
            {
                FieldId = 4,
                FieldKey = "curp",
                DataType = "TEXT",
                Scope = "USER",
                Config = JsonSerializer.Serialize(new
                {
                    is_required = false,
                    min_length = 18,
                    max_length = 18,
                    pattern = "^[A-Z]{4}[0-9]{6}[HM][A-Z]{5}[0-9]{2}$",
                    label = "CURP",
                    placeholder = "AAAA000000HDFXXX00",
                    help_text = "Clave Única de Registro de Población (opcional)"
                })
            };

            var fBirthDate = new FieldDefinition
            {
                FieldId = 5,
                FieldKey = "birth_date",
                DataType = "DATE",
                Scope = "USER",
                Config = JsonSerializer.Serialize(new
                {
                    is_required = true,
                    label = "Fecha de Nacimiento",
                    help_text = "Debes ser mayor de 18 años"
                })
            };

            var fGender = new FieldDefinition
            {
                FieldId = 6,
                FieldKey = "gender",
                DataType = "TEXT",
                Scope = "USER",
                Config = JsonSerializer.Serialize(new
                {
                    is_required = true,
                    allowed_values = new[] { "M", "F", "Otro" },
                    label = "Género",
                    help_text = "Selecciona tu género"
                })
            };

            var fRfc = new FieldDefinition 
            { 
                FieldId = 7, 
                FieldKey = "company_rfc", 
                DataType = "TEXT", 
                Scope = "USER", 
                Config = JsonSerializer.Serialize(new 
                { 
                    is_required = true,
                    min_length = 12,
                    max_length = 13,
                    pattern = "^[A-Z&Ñ]{3,4}[0-9]{6}[A-Z0-9]{3}$",
                    label = "RFC",
                    placeholder = "XAXX010101000",
                    help_text = "RFC de la empresa o persona moral"
                })
            };

            var fCiec = new FieldDefinition 
            { 
                FieldId = 8, 
                FieldKey = "clave_ciec", 
                DataType = "TEXT", 
                Scope = "APPLICATION", 
                Config = JsonSerializer.Serialize(new 
                { 
                    is_required = true,
                    min_length = 8,
                    max_length = 50,
                    label = "Clave CIEC",
                    placeholder = "Ingresa tu CIEC",
                    help_text = "Clave de Identificación Electrónica Confidencial del SAT"
                })
            };

            var fCompanyName = new FieldDefinition
            {
                FieldId = 9,
                FieldKey = "company_name",
                DataType = "TEXT",
                Scope = "USER",
                Config = JsonSerializer.Serialize(new
                {
                    is_required = true,
                    min_length = 3,
                    max_length = 200,
                    label = "Razón Social",
                    placeholder = "Nombre de la empresa",
                    help_text = "Razón social completa de la empresa"
                })
            };

            var fLoanAmount = new FieldDefinition 
            { 
                FieldId = 10, 
                FieldKey = "loan_amount", 
                DataType = "NUMBER", 
                Scope = "APPLICATION", 
                Config = JsonSerializer.Serialize(new 
                { 
                    is_required = true,
                    min_value = 10000,
                    max_value = 5000000,
                    label = "Monto Solicitado",
                    placeholder = "100000",
                    help_text = "Monto que deseas solicitar (entre $10,000 y $5,000,000)"
                })
            };

            var fVehicleCount = new FieldDefinition
            {
                FieldId = 11,
                FieldKey = "vehicle_count",
                DataType = "INTEGER",
                Scope = "APPLICATION",
                Config = JsonSerializer.Serialize(new
                {
                    is_required = true,
                    min_value = 1,
                    max_value = 100,
                    label = "Cantidad de Vehículos",
                    placeholder = "5",
                    help_text = "¿Cuántos vehículos necesitas?"
                })
            };

            var fPostalCode = new FieldDefinition
            {
                FieldId = 12,
                FieldKey = "postal_code",
                DataType = "TEXT",
                Scope = "USER",
                Config = JsonSerializer.Serialize(new
                {
                    is_required = true,
                    min_length = 5,
                    max_length = 5,
                    pattern = "^[0-9]{5}$",
                    label = "Código Postal",
                    placeholder = "01000",
                    help_text = "Código postal de tu domicilio"
                })
            };

            var fAddress = new FieldDefinition
            {
                FieldId = 13,
                FieldKey = "address",
                DataType = "TEXT",
                Scope = "USER",
                Config = JsonSerializer.Serialize(new
                {
                    is_required = true,
                    min_length = 10,
                    max_length = 200,
                    label = "Dirección",
                    placeholder = "Calle y número",
                    help_text = "Calle y número exterior/interior"
                })
            };

            // NUEVO: Campo para Accionistas (OBJECT_ARRAY)
            var fAccionistas = new FieldDefinition
            {
                FieldId = 14,
                FieldKey = "accionistas",
                DataType = "OBJECT_ARRAY",
                Scope = "APPLICATION",
                Config = JsonSerializer.Serialize(new
                {
                    is_required = true,
                    min_items = 1,
                    max_items = 10,
                    label = "Accionistas",
                    help_text = "Información de los accionistas de la empresa"
                }),
                NestedSchema = JsonSerializer.Serialize(new
                {
                    properties = new object[]
                    {
                        new
                        {
                            key = "nombre",
                            type = "TEXT",
                            required = true,
                            min_length = 3,
                            max_length = 100
                        },
                        new
                        {
                            key = "participacion",
                            type = "NUMBER",
                            required = true,
                            min_value = 0.01,
                            max_value = 100
                        },
                        new
                        {
                            key = "rfc",
                            type = "TEXT",
                            required = false,
                            min_length = 12,
                            max_length = 13,
                            pattern = "^[A-Z&Ñ]{3,4}[0-9]{6}[A-Z0-9]{3}$"
                        }
                    }
                })
            };

            db.FieldDefinitions.AddRange(
                fFullName, fEmail, fPhone, fCurp, fBirthDate, fGender,
                fRfc, fCiec, fCompanyName, fLoanAmount, fVehicleCount,
                fPostalCode, fAddress, fAccionistas); // NUEVO: Agregar accionistas

            // ==========================================
            // 4. ESTRUCTURA WORKFLOW 1: OVEX Cliente Individual
            // ==========================================
            
            // Fase 1.1: Datos Personales
            var phase1_1 = new Phase { PhaseId = 1, WorkflowId = 1, Name = "Información Personal", Order = 1 };
            db.Phases.Add(phase1_1);

            var step1_1_1 = new Step { StepId = 1, PhaseId = 1, Name = "Datos Básicos", Order = 1 };
            var step1_1_2 = new Step { StepId = 2, PhaseId = 1, Name = "Datos de Contacto", Order = 2 };
            db.Steps.AddRange(step1_1_1, step1_1_2);

            // Step 1: Datos Básicos (nombre, fecha nascimento, género)
            db.StepFields.AddRange(
                new Step_Field { StepId = 1, FieldId = 1, ConfigOverride = null }, // full_name
                new Step_Field { StepId = 1, FieldId = 5, ConfigOverride = null }, // birth_date
                new Step_Field { StepId = 1, FieldId = 6, ConfigOverride = null }  // gender
            );

            // Step 2: Datos de Contacto (email, teléfono, CURP)
            db.StepFields.AddRange(
                new Step_Field { StepId = 2, FieldId = 2, ConfigOverride = null }, // email
                new Step_Field { StepId = 2, FieldId = 3, ConfigOverride = null }, // phone
                new Step_Field { StepId = 2, FieldId = 4, ConfigOverride = JsonSerializer.Serialize(new { is_required = true }) } // curp (requerido)
            );

            // Fase 1.2: Dirección
            var phase1_2 = new Phase { PhaseId = 2, WorkflowId = 1, Name = "Datos de Domicilio", Order = 2 };
            db.Phases.Add(phase1_2);

            var step1_2_1 = new Step { StepId = 3, PhaseId = 2, Name = "Dirección", Order = 1 };
            db.Steps.Add(step1_2_1);

            db.StepFields.AddRange(
                new Step_Field { StepId = 3, FieldId = 12, ConfigOverride = null }, // postal_code
                new Step_Field { StepId = 3, FieldId = 13, ConfigOverride = null }  // address
            );

            // ==========================================
            // 5. ESTRUCTURA WORKFLOW 2: OVEX Flotilla
            // ==========================================
            
            // Fase 2.1: Datos de Empresa
            var phase2_1 = new Phase { PhaseId = 3, WorkflowId = 2, Name = "Información Empresarial", Order = 1 };
            db.Phases.Add(phase2_1);

            var step2_1_1 = new Step { StepId = 4, PhaseId = 3, Name = "Datos Fiscales", Order = 1 };
            var step2_1_2 = new Step { StepId = 5, PhaseId = 3, Name = "Datos de Representante", Order = 2 };
            db.Steps.AddRange(step2_1_1, step2_1_2);

            // Step 4: Datos Fiscales (RFC, CIEC, razón social)
            db.StepFields.AddRange(
                new Step_Field { StepId = 4, FieldId = 7, ConfigOverride = null }, // company_rfc
                new Step_Field { StepId = 4, FieldId = 8, ConfigOverride = JsonSerializer.Serialize(new { is_required = true, min_length = 10 }) }, // clave_ciec
                new Step_Field { StepId = 4, FieldId = 9, ConfigOverride = null }  // company_name
            );

            // Step 5: Datos de Representante
            db.StepFields.AddRange(
                new Step_Field { StepId = 5, FieldId = 1, ConfigOverride = JsonSerializer.Serialize(new { label = "Nombre del Representante Legal" }) }, // full_name
                new Step_Field { StepId = 5, FieldId = 2, ConfigOverride = null }, // email
                new Step_Field { StepId = 5, FieldId = 3, ConfigOverride = null }  // phone
            );

            // Fase 2.2: Solicitud
            var phase2_2 = new Phase { PhaseId = 4, WorkflowId = 2, Name = "Detalles de Solicitud", Order = 2 };
            db.Phases.Add(phase2_2);

            var step2_2_1 = new Step { StepId = 6, PhaseId = 4, Name = "Información de Flotilla", Order = 1 };
            db.Steps.Add(step2_2_1);

            db.StepFields.AddRange(
                new Step_Field { StepId = 6, FieldId = 11, ConfigOverride = null }, // vehicle_count
                new Step_Field { StepId = 6, FieldId = 10, ConfigOverride = JsonSerializer.Serialize(new { label = "Monto Total Estimado" }) } // loan_amount
            );

            // NUEVO: Fase 2.3: Accionistas
            var phase2_3 = new Phase { PhaseId = 7, WorkflowId = 2, Name = "Estructura Accionaria", Order = 3 };
            db.Phases.Add(phase2_3);

            var step2_3_1 = new Step { StepId = 10, PhaseId = 7, Name = "Accionistas", Order = 1 };
            db.Steps.Add(step2_3_1);

            db.StepFields.Add(
                new Step_Field 
                { 
                    StepId = 10, 
                    FieldId = 14, // accionistas
                    ConfigOverride = JsonSerializer.Serialize(new 
                    { 
                        label = "Accionistas de la Empresa",
                        help_text = "Agregar la información de todos los accionistas que posean más del 10% de participación"
                    }) 
                }
            );

            // ==========================================
            // 6. ESTRUCTURA WORKFLOW 3: PCH Persona Moral
            // ==========================================
            
            // Fase 3.1: Información Corporativa
            var phase3_1 = new Phase { PhaseId = 5, WorkflowId = 3, Name = "Información Corporativa", Order = 1 };
            db.Phases.Add(phase3_1);

            var step3_1_1 = new Step { StepId = 7, PhaseId = 5, Name = "Datos Fiscales de la Empresa", Order = 1 };
            var step3_1_2 = new Step { StepId = 8, PhaseId = 5, Name = "Representante Legal", Order = 2 };
            db.Steps.AddRange(step3_1_1, step3_1_2);

            // Step 7: Datos Fiscales
            db.StepFields.AddRange(
                new Step_Field { StepId = 7, FieldId = 9, ConfigOverride = null }, // company_name
                new Step_Field { StepId = 7, FieldId = 7, ConfigOverride = null }, // company_rfc
                new Step_Field { StepId = 7, FieldId = 8, ConfigOverride = null }  // clave_ciec
            );

            // Step 8: Representante Legal
            db.StepFields.AddRange(
                new Step_Field { StepId = 8, FieldId = 1, ConfigOverride = JsonSerializer.Serialize(new { label = "Nombre del Representante Legal" }) }, // full_name
                new Step_Field { StepId = 8, FieldId = 2, ConfigOverride = JsonSerializer.Serialize(new { label = "Email Corporativo" }) }, // email
                new Step_Field { StepId = 8, FieldId = 3, ConfigOverride = null }, // phone
                new Step_Field { StepId = 8, FieldId = 4, ConfigOverride = JsonSerializer.Serialize(new { is_required = true }) } // curp
            );

            // Fase 3.2: Solicitud de Financiamiento
            var phase3_2 = new Phase { PhaseId = 6, WorkflowId = 3, Name = "Solicitud de Financiamiento", Order = 2 };
            db.Phases.Add(phase3_2);

            var step3_2_1 = new Step { StepId = 9, PhaseId = 6, Name = "Monto y Propósito", Order = 1 };
            db.Steps.Add(step3_2_1);

            db.StepFields.AddRange(
                new Step_Field { StepId = 9, FieldId = 10, ConfigOverride = JsonSerializer.Serialize(new { label = "Monto de Financiamiento Solicitado" }) } // loan_amount
            );

            // ==========================================
            // 7. ESTRATEGIAS (InstanceActionStrategies)
            // ==========================================
            db.InstanceActionStrategies.AddRange(
                new InstanceActionStrategy 
                { 
                    ActionKey = "CREATE_INTERNAL_PROSPECT", 
                    ImplementationType = "INTERNAL_CODE", 
                    ImplementationDetails = JsonSerializer.Serialize(new 
                    { 
                        class_name = "InitialCreationStrategy" 
                    })
                },
                
                new InstanceActionStrategy 
                { 
                    ActionKey = "CALL_BURO_CREDITO", 
                    ImplementationType = "EXTERNAL_API", 
                    ImplementationDetails = JsonSerializer.Serialize(new 
                    { 
                        url = "https://api.burodecredito.com.mx/v1/credit-score", 
                        method = "POST", 
                        auth_header = "Bearer BURO_API_KEY",
                        timeout_seconds = 45,
                        completion_event = "CreditCheckComplete"
                    })
                },
                
                new InstanceActionStrategy 
                { 
                    ActionKey = "CALL_SAT_SYN", 
                    ImplementationType = "EXTERNAL_API", 
                    ImplementationDetails = JsonSerializer.Serialize(new 
                    { 
                        url = "https://api.syntage.com/v2/sat/tax-data", 
                        method = "POST",
                        auth_header = "Bearer SYNTAGE_API_KEY",
                        timeout_seconds = 60,
                        completion_event = "SatCheckComplete"
                    })
                },
                
                new InstanceActionStrategy 
                { 
                    ActionKey = "PROMOTE_TO_GOLDEN_RECORD", 
                    ImplementationType = "INTERNAL_CODE", 
                    ImplementationDetails = JsonSerializer.Serialize(new 
                    { 
                        class_name = "PromoteToGoldenRecordStrategy",
                        notify_user = true 
                    }) 
                }
            );

            // ==========================================
            // 8. REGLAS DE NEGOCIO (Rules)
            // ==========================================
            db.Rules.AddRange(
                new Rule
                {
                    RuleId = 1,
                    WorkflowId = 1,
                    TriggerEvent = "UserRegistered",
                    ConditionExpression = "true",
                    ActionKeyOnTrue = "CREATE_INTERNAL_PROSPECT"
                },
                new Rule
                {
                    RuleId = 2,
                    WorkflowId = 2,
                    TriggerEvent = "UserRegistered",
                    ConditionExpression = "true",
                    ActionKeyOnTrue = "CREATE_INTERNAL_PROSPECT"
                },
                new Rule
                {
                    RuleId = 3,
                    WorkflowId = 2,
                    TriggerEvent = "StepDataSubmitted",
                    ConditionExpression = "data.company_rfc == 'FLOT101010ABC'",
                    ActionKeyOnTrue = "CALL_BURO_CREDITO"
                },
                new Rule
                {
                    RuleId = 4,
                    WorkflowId = 3,
                    TriggerEvent = "UserRegistered",
                    ConditionExpression = "true",
                    ActionKeyOnTrue = "CREATE_INTERNAL_PROSPECT"
                }
            );

            // ==========================================
            // 9. DATOS DE PRUEBA
            // ==========================================
            var dummyUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var dummyProspectId = Guid.Parse("11111111-1111-1111-1111-111111111111");

            db.Users.Add(new User
            {
                UserId = dummyUserId,
                Email = "test@ovex.com",
                CreatedAt = DateTime.UtcNow
            });

            db.Prospects.Add(new Prospect
            {
                ProspectId = dummyProspectId,
                UserId = dummyUserId,
                WorkflowId = 2,
                Status = "IN_PROGRESS",
                CurrentStepId = 4,
                CreatedAt = DateTime.UtcNow
            });

            db.ProspectData.Add(new ProspectData
            {
                ProspectId = dummyProspectId,
                Data = JsonSerializer.Serialize(new 
                { 
                    email = "test@ovex.com",
                    full_name = "Juan Pérez Test"
                })
            });

            db.ValidationProviders.Add(new ValidationProvider
            {
                ProviderId = 1,
                ProviderKey = "CHECK_DUPLICATE_EMAIL",
                ProviderType = "INTERNAL_CODE",
                ConfigJson = JsonSerializer.Serialize(new { CheckScope = "ALL_WORKFLOWS" }),
                IsActive = true
            });

            db.ValidationPipelines.Add(new ValidationPipeline
            {
                PipelineId = 1,
                PipelineKey = "USER_REGISTRATION_PIPELINE",
                Name = "Pipeline para validar duplicados al registrar usuarios",
                TriggerContext = "PRE_USER_REGISTRATION",
                IsActive = true,
                StopOnFirstFailure = true,
                RequireAllPass = true,
                Priority = 100
            });

            db.PipelineSteps.Add(new PipelineStep
            {
                PipelineStepId = 1,
                PipelineId = 1, // Asociado al pipeline "USER_REGISTRATION_PIPELINE"
                ProviderId = 1, // ID del proveedor "CHECK_DUPLICATE_EMAIL"
                ExecutionOrder = 1,
                IsActive = true,
                OnFailureAction = "STOP",
                ConfigOverrideJson = JsonSerializer.Serialize(new { CheckScope = "ALL_WORKFLOWS" })
            });

            db.SaveChanges();
            Console.WriteLine("--> [SEEDER] Carga de datos completada exitosamente.");
            Console.WriteLine($"--> [SEEDER] - Workflow 1: OVEX Cliente Individual (3 steps en 2 fases)");
            Console.WriteLine($"--> [SEEDER] - Workflow 2: OVEX Flotilla Empresarial (4 steps en 3 fases) - INCLUYE ACCIONISTAS");
            Console.WriteLine($"--> [SEEDER] - Workflow 3: PCH Persona Moral (3 steps en 2 fases)");
            Console.WriteLine($"--> [SEEDER] Usuario de prueba: {dummyUserId}");
            Console.WriteLine($"--> [SEEDER] Prospecto de prueba: {dummyProspectId}");
        }
    }
}
