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
            var fFullName = new FieldDefinition { FieldId = 1, FieldKey = "full_name", DataType = "TEXT", Scope = "USER", Config = "{\"is_required\": true}" };
            var fEmail = new FieldDefinition { FieldId = 2, FieldKey = "email", DataType = "TEXT", Scope = "USER", Config = "{\"is_required\": true, \"regex\": \"email\"}" };

            // Campos específicos de Flotilla
            var fCiec = new FieldDefinition { FieldId = 3, FieldKey = "clave_ciec", DataType = "TEXT", Scope = "APPLICATION", Config = "{\"is_required\": true}" };
            var fRfc = new FieldDefinition { FieldId = 4, FieldKey = "company_rfc", DataType = "TEXT", Scope = "USER", Config = "{\"is_required\": true, \"regex\": \"RFC\"}" };
            var fMonto = new FieldDefinition { FieldId = 5, FieldKey = "loan_amount", DataType = "NUMBER", Scope = "APPLICATION", Config = "{\"min\": 10000}" };

            db.FieldDefinitions.AddRange(fFullName, fEmail, fCiec, fRfc, fMonto);

            // ==========================================
            // 4. ESTRUCTURA (Phases, Steps & Step_Fields)
            // ==========================================

            // Estructura para Workflow 20 (Flotilla)
            var phaseAnalysis = new Phase { PhaseId = 100, WorkflowId = 20, Name = "Análisis Preliminar", Order = 1 };
            db.Phases.Add(phaseAnalysis);

            // Paso 101: Datos Fiscales
            var stepFiscal = new Step { StepId = 101, PhaseId = 100, Name = "Datos Fiscales y RFC", Order = 1 };
            db.Steps.Add(stepFiscal);

            // Asociación: El Paso 101 pide RFC (4) y CIEC (3)
            db.StepFields.AddRange(
                new Step_Field { StepId = 101, FieldId = 4 }, // company_rfc
                new Step_Field { StepId = 101, FieldId = 3 }  // clave_ciec
            );

            // ==========================================
            // 5. ESTRATEGIAS (InstanceActionStrategies)
            // ==========================================
            db.InstanceActionStrategies.AddRange(
                // Legacy
                new InstanceActionStrategy { ActionKey = "CALL_LEGACY_API", ImplementationType = "EXTERNAL_API", ImplementationDetails = JsonSerializer.Serialize(new { url = "https://legacy.ovex.com/api/register", method = "POST" }) },
                new InstanceActionStrategy { ActionKey = "REDIRECT_CLIENT", ImplementationType = "INTERNAL_CODE", ImplementationDetails = JsonSerializer.Serialize(new { class_name = "RedirectStrategy", target_url = "https://legacy.ovex.com/continue" }) },

                // Flotilla
                new InstanceActionStrategy { ActionKey = "CREATE_INTERNAL_PROSPECT", ImplementationType = "INTERNAL_CODE", ImplementationDetails = JsonSerializer.Serialize(new { class_name = "InitialCreationStrategy" }) },
                new InstanceActionStrategy { ActionKey = "CALL_BURO_CREDITO", ImplementationType = "EXTERNAL_API", ImplementationDetails = JsonSerializer.Serialize(new { url = "https://api.burodecredito.com.mx/score", method = "POST", auth_header = "Bearer KEY" }) },
                new InstanceActionStrategy { ActionKey = "CALL_SAT_SYN", ImplementationType = "EXTERNAL_API", ImplementationDetails = JsonSerializer.Serialize(new { url = "https://api.syntage.com/tax-data", method = "GET" }) }
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
                }
            );

            // ==========================================
            // 7. DUMMY DATA (Para pruebas de pasos intermedios)
            // ==========================================
            // Creamos un prospecto que YA existe, asignado al Flujo 20 y situado en el Paso 101.
            var dummyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            db.Prospects.Add(new Prospect
            {
                ProspectId = dummyId,
                UserId = Guid.NewGuid(),
                WorkflowId = 20, // Flotilla
                Status = "IN_PROGRESS",
                CurrentStepId = 101, // Está en "Datos Fiscales"
                CreatedAt = DateTime.UtcNow
            });
            // Creamos también el registro de datos asociado (vacío al inicio)
            db.ProspectData.Add(new ProspectData
            {
                ProspectId = dummyId,
                Data = "{}"
            });

            db.SaveChanges();
            Console.WriteLine("--> [SEEDER] Carga de datos completada exitosamente.");
        }
    }
}
