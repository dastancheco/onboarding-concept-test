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
            // Evitar duplicados si la BD en memoria persiste (rara vez pasa, pero es buena práctica)
            if (db.Workflows.Any()) return;

            Console.WriteLine("--> [SEEDER] Inicializando Configuración de OVEX (Legacy & Flotilla)...");

            // ==========================================
            // 1. WORKFLOWS (Los Contenedores)
            // ==========================================
            var wfLegacy = new Workflow { WorkflowId = 10, WorkflowType = "PROSPECT", SubTypeKey = "OVEX_PF_CLIENT", Name = "Ovex PF Legacy (Híbrido)", IsActive = true };
            var wfFlotilla = new Workflow { WorkflowId = 20, WorkflowType = "PROSPECT", SubTypeKey = "OVEX_FLOTILLA", Name = "Ovex Flotilla (Nativo)", IsActive = true };

            db.Workflows.AddRange(wfLegacy, wfFlotilla);

            // ==========================================
            // 2. RUTEO INICIAL (RoutingRules)
            // ==========================================
            db.WorkflowRoutingRules.AddRange(
                // Regla 1: Cliente Normal -> Legacy (ID 10)
                new WorkflowRoutingRule
                {
                    RoutingRuleId = 1,
                    TargetWorkflowType = "PROSPECT",
                    Priority = 10,
                    ConditionExpression = "input.app_id == 'OVEX' && input.client_type == 'CLIENT'",
                    TargetWorkflowId = 10
                },
                // Regla 2: Cliente Flotilla -> Nativo (ID 20)
                new WorkflowRoutingRule
                {
                    RoutingRuleId = 2,
                    TargetWorkflowType = "PROSPECT",
                    Priority = 20,
                    ConditionExpression = "input.app_id == 'OVEX' && input.client_type == 'FLOTILLA_EMP'",
                    TargetWorkflowId = 20
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
                    max_length = 100
                })
            };
            
            var fEmail = new FieldDefinition 
            { 
                FieldId = 2, 
                FieldKey = "email", 
                DataType = "TEXT", 
                Scope = "USER", 
                Config = JsonSerializer.Serialize(new 
                { 
                    is_required = true,
                    validator = "EMAIL"
                })
            };

            // Campos específicos de Flotilla
            var fCiec = new FieldDefinition 
            { 
                FieldId = 3, 
                FieldKey = "clave_ciec", 
                DataType = "TEXT", 
                Scope = "APPLICATION", 
                Config = JsonSerializer.Serialize(new 
                { 
                    is_required = true,
                    min_length = 8,
                    max_length = 50
                })
            };
            
            var fRfc = new FieldDefinition 
            { 
                FieldId = 4, 
                FieldKey = "company_rfc", 
                DataType = "TEXT", 
                Scope = "USER", 
                Config = JsonSerializer.Serialize(new 
                { 
                    is_required = true,
                    validator = "RFC"
                })
            };
            
            var fMonto = new FieldDefinition 
            { 
                FieldId = 5, 
                FieldKey = "loan_amount", 
                DataType = "NUMBER", 
                Scope = "APPLICATION", 
                Config = JsonSerializer.Serialize(new 
                { 
                    is_required = true,
                    min = 10000,
                    max = 5000000
                })
            };

            // Nuevos campos con validaciones específicas
            var fPhone = new FieldDefinition
            {
                FieldId = 6,
                FieldKey = "phone",
                DataType = "TEXT",
                Scope = "USER",
                Config = JsonSerializer.Serialize(new
                {
                    is_required = true,
                    validator = "PHONE"
                })
            };

            var fCurp = new FieldDefinition
            {
                FieldId = 7,
                FieldKey = "curp",
                DataType = "TEXT",
                Scope = "USER",
                Config = JsonSerializer.Serialize(new
                {
                    is_required = false, // Opcional
                    validator = "CURP"
                })
            };

            var fBirthDate = new FieldDefinition
            {
                FieldId = 8,
                FieldKey = "birth_date",
                DataType = "DATE",
                Scope = "USER",
                Config = JsonSerializer.Serialize(new
                {
                    is_required = true,
                    validator = "DATE"
                })
            };

            var fGender = new FieldDefinition
            {
                FieldId = 9,
                FieldKey = "gender",
                DataType = "TEXT",
                Scope = "USER",
                Config = JsonSerializer.Serialize(new
                {
                    is_required = true,
                    allowed_values = new[] { "M", "F", "OTHER" }
                })
            };

            db.FieldDefinitions.AddRange(fFullName, fEmail, fCiec, fRfc, fMonto, fPhone, fCurp, fBirthDate, fGender);

            // ==========================================
            // 4. ESTRUCTURA (Phases, Steps & Step_Fields)
            // ==========================================

            // Estructura para Workflow 20 (Flotilla)
            var phaseAnalysis = new Phase { PhaseId = 100, WorkflowId = 20, Name = "Análisis Preliminar", Order = 1 };
            db.Phases.Add(phaseAnalysis);

            // Paso 101: Datos Fiscales
            var stepFiscal = new Step { StepId = 101, PhaseId = 100, Name = "Datos Fiscales y RFC", Order = 1 };
            db.Steps.Add(stepFiscal);

            // Paso 102: Datos Personales
            var stepPersonal = new Step { StepId = 102, PhaseId = 100, Name = "Datos Personales", Order = 2 };
            db.Steps.Add(stepPersonal);

            // Asociación: El Paso 101 pide RFC (4) y CIEC (3)
            db.StepFields.AddRange(
                new Step_Field 
                { 
                    StepId = 101, 
                    FieldId = 4, // company_rfc
                    ConfigOverride = null // Usa config por defecto
                },
                new Step_Field 
                { 
                    StepId = 101, 
                    FieldId = 3, // clave_ciec
                    ConfigOverride = JsonSerializer.Serialize(new 
                    { 
                        is_required = true,
                        min_length = 10, // Override: más estricto que el default (8)
                        max_length = 30
                    })
                }
            );

            // Asociación: El Paso 102 pide datos personales
            db.StepFields.AddRange(
                new Step_Field 
                { 
                    StepId = 102, 
                    FieldId = 1, // full_name
                    ConfigOverride = null
                },
                new Step_Field 
                { 
                    StepId = 102, 
                    FieldId = 2, // email
                    ConfigOverride = null
                },
                new Step_Field 
                { 
                    StepId = 102, 
                    FieldId = 6, // phone
                    ConfigOverride = null
                },
                new Step_Field 
                { 
                    StepId = 102, 
                    FieldId = 7, // curp
                    ConfigOverride = JsonSerializer.Serialize(new 
                    { 
                        is_required = true // Override: hacerlo obligatorio en este paso
                    })
                },
                new Step_Field 
                { 
                    StepId = 102, 
                    FieldId = 8, // birth_date
                    ConfigOverride = null
                },
                new Step_Field 
                { 
                    StepId = 102, 
                    FieldId = 9, // gender
                    ConfigOverride = null
                }
            );

            // ==========================================
            // 5. ESTRATEGIAS (InstanceActionStrategies)
            // ==========================================
            db.InstanceActionStrategies.AddRange(
                // Legacy
                new InstanceActionStrategy 
                { 
                    ActionKey = "CALL_LEGACY_API", 
                    ImplementationType = "EXTERNAL_API", 
                    ImplementationDetails = JsonSerializer.Serialize(new 
                    { 
                        url = "https://legacy.ovex.com/api/register", 
                        method = "POST",
                        timeout_seconds = 30,
                        auth_header = "Bearer LEGACY_API_KEY",
                        completion_event = "LegacyApiComplete"
                    })
                },
                new InstanceActionStrategy 
                { 
                    ActionKey = "REDIRECT_CLIENT", 
                    ImplementationType = "INTERNAL_CODE", 
                    ImplementationDetails = JsonSerializer.Serialize(new 
                    { 
                        class_name = "RedirectStrategy", 
                        target_url = "https://legacy.ovex.com/continue" 
                    })
                },

                // Flotilla
                new InstanceActionStrategy 
                { 
                    ActionKey = "CREATE_INTERNAL_PROSPECT", 
                    ImplementationType = "INTERNAL_CODE", 
                    ImplementationDetails = JsonSerializer.Serialize(new 
                    { 
                        class_name = "InitialCreationStrategy" 
                    })
                },
                
                // SPRINT 2: Configuración completa para API de Buró
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
                        completion_event = "CreditCheckComplete",
                        response_mapping = new 
                        {
                            buro_score = "score",
                            buro_status = "status",
                            buro_response_id = "transaction_id"
                        },
                        additional_fields = new 
                        {
                            source = "onboarding-system",
                            version = "1.0"
                        }
                    })
                },
                
                // SPRINT 2: Configuración completa para API de SAT (Syntage)
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
                        completion_event = "SatCheckComplete",
                        response_mapping = new 
                        {
                            sat_status = "data.status",
                            tax_regime = "data.tax_regime",
                            sat_verified_at = "data.verification_date"
                        }
                    })
                },
                
                // SPRINT 1 - Opciones Adicionales
                new InstanceActionStrategy 
                { 
                    ActionKey = "PROMOTE_TO_GOLDEN_RECORD", 
                    ImplementationType = "INTERNAL_CODE", 
                    ImplementationDetails = JsonSerializer.Serialize(new 
                    { 
                        class_name = "PromoteToGoldenRecordStrategy",
                        notify_user = true 
                    }) 
                },
                new InstanceActionStrategy 
                { 
                    ActionKey = "MOVE_TO_PENDING_REVIEW", 
                    ImplementationType = "INTERNAL_CODE", 
                    ImplementationDetails = JsonSerializer.Serialize(new 
                    { 
                        class_name = "UpdateProspectStatusStrategy",
                        target_status = "PENDING_REVIEW",
                        reason = "Datos completos, requiere revisión manual"
                    }) 
                },
                new InstanceActionStrategy 
                { 
                    ActionKey = "APPROVE_PROSPECT", 
                    ImplementationType = "INTERNAL_CODE", 
                    ImplementationDetails = JsonSerializer.Serialize(new 
                    { 
                        class_name = "UpdateProspectStatusStrategy",
                        target_status = "APPROVED",
                        reason = "Aprobado automáticamente por reglas de negocio"
                    }) 
                }
            );

            // ==========================================
            // 6. REGLAS DE NEGOCIO (Rules)
            // ==========================================
            db.Rules.AddRange(
                // --- Reglas Legacy (WF 10) ---
                new Rule
                {
                    RuleId = 1,
                    WorkflowId = 10,
                    TriggerEvent = "UserRegistered",
                    ConditionExpression = "true", // Siempre ejecuta al entrar
                    ActionKeyOnTrue = "CALL_LEGACY_API"
                },

                // --- Reglas Flotilla (WF 20) ---
                // 1. Al inicio -> Crear instancia interna
                new Rule
                {
                    RuleId = 10,
                    WorkflowId = 20,
                    TriggerEvent = "UserRegistered",
                    ConditionExpression = "true",
                    ActionKeyOnTrue = "CREATE_INTERNAL_PROSPECT"
                },

                // 2. Al subir Datos Fiscales (Step 101) -> Validar RFC específico y llamar Buró
                // Nota: Usamos '==' simple que soporta nuestro RuleEngine básico.
                new Rule
                {
                    RuleId = 11,
                    WorkflowId = 20,
                    TriggerEvent = "StepDataSubmitted",
                    ConditionExpression = "data.company_rfc == 'FLOT101010ABC'",
                    ActionKeyOnTrue = "CALL_BURO_CREDITO"
                },

                // 3. Encadenamiento: Si Buró responde OK -> Llamar SAT
                new Rule
                {
                    RuleId = 12,
                    WorkflowId = 20,
                    TriggerEvent = "CreditCheckComplete",
                    ConditionExpression = "data.buro_score >= 600",
                    ActionKeyOnTrue = "CALL_SAT_SYN"
                },

                // 4. NUEVA: Al aprobar prospecto -> Promover a Golden Record
                new Rule
                {
                    RuleId = 13,
                    WorkflowId = 20,
                    TriggerEvent = "ProspectApproved",
                    ConditionExpression = "true",
                    ActionKeyOnTrue = "PROMOTE_TO_GOLDEN_RECORD"
                },

                // 5. NUEVA: Después de SAT exitoso -> Mover a revisión
                new Rule
                {
                    RuleId = 14,
                    WorkflowId = 20,
                    TriggerEvent = "SatCheckComplete",
                    ConditionExpression = "data.sat_status == 'ACTIVE'",
                    ActionKeyOnTrue = "MOVE_TO_PENDING_REVIEW"
                }
            );

            // ==========================================
            // 7. DUMMY DATA (Para pruebas de pasos intermedios)
            // ==========================================
            // Creamos un prospecto que YA existe, asignado al Flujo 20 y situado en el Paso 101.
            var dummyUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var dummyProspectId = Guid.Parse("11111111-1111-1111-1111-111111111111");

            // Usuario de prueba
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
                WorkflowId = 20, // Flotilla
                Status = "IN_PROGRESS", // Estado válido según ProspectStateMachine
                CurrentStepId = 101, // Está en "Datos Fiscales"
                CreatedAt = DateTime.UtcNow
            });

            // Creamos también el registro de datos asociado con datos de ejemplo
            db.ProspectData.Add(new ProspectData
            {
                ProspectId = dummyProspectId,
                Data = JsonSerializer.Serialize(new 
                { 
                    email = "test@ovex.com",
                    full_name = "Juan Pérez Test"
                })
            });

            db.SaveChanges();
            Console.WriteLine("--> [SEEDER] Carga de datos completada exitosamente.");
            Console.WriteLine($"--> [SEEDER] Usuario de prueba creado: {dummyUserId}");
            Console.WriteLine($"--> [SEEDER] Prospecto de prueba creado: {dummyProspectId}");
        }
    }
}
