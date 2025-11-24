# ?? Guía de Configuración de Validaciones

## Introducción

El sistema de validaciones es completamente **configurable desde la base de datos** mediante dos campos:

1. **`FieldDefinition.Config`**: Configuración base del campo
2. **`Step_Field.ConfigOverride`**: Sobrescribe la configuración para un paso específico

Ambos campos almacenan JSON con las reglas de validación.

---

## ?? Opciones de Configuración

### **1. Requerido (is_required)**

Indica si el campo es obligatorio.

```json
{
  "is_required": true
}
```

**Ejemplo:**
```json
{
  "FieldKey": "email",
  "Config": "{\"is_required\": true}"
}
```

---

### **2. Rango de Valores (min, max)**

Para campos numéricos (`DataType = "NUMBER"`).

```json
{
  "min": 10000,
  "max": 5000000
}
```

**Ejemplo:**
```json
{
  "FieldKey": "loan_amount",
  "DataType": "NUMBER",
  "Config": "{\"min\": 10000, \"max\": 5000000}"
}
```

---

### **3. Longitud de Texto (min_length, max_length)**

Para campos de texto (`DataType = "TEXT"`).

```json
{
  "min_length": 3,
  "max_length": 100
}
```

**Ejemplo:**
```json
{
  "FieldKey": "full_name",
  "DataType": "TEXT",
  "Config": "{\"min_length\": 3, \"max_length\": 100}"
}
```

---

### **4. Validadores Específicos (validator)**

Validadores predefinidos para formatos comunes.

```json
{
  "validator": "RFC"
}
```

#### **Validadores Disponibles:**

| Validador | Descripción | Ejemplo |
|-----------|-------------|---------|
| `RFC` | RFC mexicano (12 o 13 caracteres) | `ABC123456XXX` |
| `CURP` | CURP mexicana (18 caracteres) | `PEGJ900101HDFRRN01` |
| `EMAIL` | Email válido | `usuario@dominio.com` |
| `PHONE` | Teléfono mexicano (10 dígitos) | `5512345678` |
| `POSTAL_CODE` | Código postal (5 dígitos) | `01000` |
| `URL` | URL válida (http/https) | `https://example.com` |
| `DATE` | Fecha válida | `2024-01-15` |
| `REGEX` | Patrón personalizado | Ver abajo |

**Ejemplo RFC:**
```json
{
  "FieldKey": "company_rfc",
  "DataType": "TEXT",
  "Config": "{\"is_required\": true, \"validator\": \"RFC\"}"
}
```

**Ejemplo Email:**
```json
{
  "FieldKey": "email",
  "DataType": "TEXT",
  "Config": "{\"is_required\": true, \"validator\": \"EMAIL\"}"
}
```

---

### **5. Validador REGEX Personalizado**

Para validaciones con expresiones regulares personalizadas.

```json
{
  "validator": "REGEX",
  "pattern": "^[A-Z]{3}\\d{6}$"
}
```

**Ejemplo: Matrícula de Vehículo**
```json
{
  "FieldKey": "license_plate",
  "DataType": "TEXT",
  "Config": "{\"validator\": \"REGEX\", \"pattern\": \"^[A-Z]{3}\\\\d{3}$\"}"
}
```

---

### **6. Valores Permitidos (allowed_values)**

Lista cerrada de valores aceptados (enum).

```json
{
  "allowed_values": ["M", "F", "OTHER"]
}
```

**Ejemplo:**
```json
{
  "FieldKey": "gender",
  "DataType": "TEXT",
  "Config": "{\"allowed_values\": [\"M\", \"F\", \"OTHER\"]}"
}
```

---

## ?? ConfigOverride: Sobrescribir por Paso

`Step_Field.ConfigOverride` permite **personalizar la configuración para un paso específico** sin modificar la definición global del campo.

### **Casos de Uso:**

#### **1. Hacer un campo opcional obligatorio**

**FieldDefinition (Base):**
```json
{
  "FieldKey": "curp",
  "Config": "{\"is_required\": false, \"validator\": \"CURP\"}"
}
```

**Step_Field (Override):**
```json
{
  "StepId": 102,
  "FieldId": 7,
  "ConfigOverride": "{\"is_required\": true}"
}
```

**Resultado:** CURP es opcional globalmente, pero obligatorio en el Paso 102.

---

#### **2. Cambiar rangos para un paso específico**

**FieldDefinition (Base):**
```json
{
  "FieldKey": "loan_amount",
  "Config": "{\"min\": 10000, \"max\": 5000000}"
}
```

**Step_Field (Override):**
```json
{
  "StepId": 105,
  "FieldId": 5,
  "ConfigOverride": "{\"min\": 50000, \"max\": 1000000}"
}
```

**Resultado:** En el Paso 105, el monto mínimo es $50,000 (más estricto).

---

#### **3. Cambiar longitud mínima**

**FieldDefinition (Base):**
```json
{
  "FieldKey": "clave_ciec",
  "Config": "{\"min_length\": 8, \"max_length\": 50}"
}
```

**Step_Field (Override):**
```json
{
  "StepId": 101,
  "FieldId": 3,
  "ConfigOverride": "{\"min_length\": 10}"
}
```

**Resultado:** En el Paso 101, la CIEC debe tener al menos 10 caracteres.

---

## ?? Ejemplos Completos

### **Ejemplo 1: Campo de Email**

```sql
INSERT INTO Config.FieldDefinitions (FieldId, FieldKey, DataType, Scope, Config)
VALUES (
  2, 
  'email', 
  'TEXT', 
  'USER',
  '{"is_required": true, "validator": "EMAIL"}'
);

INSERT INTO Config.Step_Fields (StepId, FieldId, ConfigOverride)
VALUES (102, 2, NULL); -- Sin override, usa config base
```

---

### **Ejemplo 2: Campo de RFC con Override**

```sql
-- Definición base
INSERT INTO Config.FieldDefinitions (FieldId, FieldKey, DataType, Scope, Config)
VALUES (
  4, 
  'company_rfc', 
  'TEXT', 
  'USER',
  '{"is_required": true, "validator": "RFC"}'
);

-- Uso en Paso 101 (sin override)
INSERT INTO Config.Step_Fields (StepId, FieldId, ConfigOverride)
VALUES (101, 4, NULL);

-- Uso en Paso 200 (hacerlo opcional)
INSERT INTO Config.Step_Fields (StepId, FieldId, ConfigOverride)
VALUES (200, 4, '{"is_required": false}');
```

---

### **Ejemplo 3: Campo de Monto con Rangos**

```sql
INSERT INTO Config.FieldDefinitions (FieldId, FieldKey, DataType, Scope, Config)
VALUES (
  5, 
  'loan_amount', 
  'NUMBER', 
  'APPLICATION',
  '{"is_required": true, "min": 10000, "max": 5000000}'
);
```

---

### **Ejemplo 4: Campo con Valores Permitidos**

```sql
INSERT INTO Config.FieldDefinitions (FieldId, FieldKey, DataType, Scope, Config)
VALUES (
  9, 
  'gender', 
  'TEXT', 
  'USER',
  '{"is_required": true, "allowed_values": ["M", "F", "OTHER"]}'
);
```

---

## ?? Orden de Prioridad

Al evaluar validaciones, el sistema sigue este orden:

1. **ConfigOverride** (si existe) ? Máxima prioridad
2. **Config** (base) ? Configuración por defecto
3. **Validación implícita por DataType** ? Fallback

**Ejemplo:**
```json
// Config base
{
  "is_required": false,
  "min_length": 5
}

// ConfigOverride
{
  "is_required": true
}

// Configuración efectiva
{
  "is_required": true,  // De override
  "min_length": 5       // De base (no se sobrescribe)
}
```

---

## ? Códigos de Error

| Código | Descripción | Cuándo se Genera |
|--------|-------------|------------------|
| `REQUIRED` | Campo obligatorio faltante | `is_required: true` y valor vacío |
| `INVALID_TYPE` | Tipo de dato incorrecto | Valor no coincide con `DataType` |
| `OUT_OF_RANGE` | Fuera de rango | Número fuera de `min`/`max` |
| `INVALID_LENGTH` | Longitud incorrecta | Texto fuera de `min_length`/`max_length` |
| `INVALID_FORMAT` | Formato inválido | Validador específico falló |
| `INVALID_VALUE` | Valor no permitido | No está en `allowed_values` |

---

## ?? Probar Validaciones

Usa el archivo `Onboarding.Api.http` para probar cada validación:

```http
### Probar RFC inválido
POST http://localhost:5014/api/events/push
Content-Type: application/json

{
  "eventType": "StepDataSubmitted",
  "payloadJson": "{ \"prospect_id\": \"...\", \"company_rfc\": \"INVALIDO\" }"
}
```

---

## ?? Agregar Nuevos Validadores

### **Paso 1: Crear Validador**

```csharp
public class MyCustomValidator : IFieldValidator
{
    public string ValidatorType => "MY_CUSTOM";

    public bool IsValid(string? value, object? config = null)
    {
        // Tu lógica aquí
        return true;
    }

    public string GetErrorMessage(string fieldKey, object? config = null)
    {
        return $"El campo '{fieldKey}' no es válido.";
    }
}
```

### **Paso 2: Registrar en DI**

```csharp
services.AddSingleton<IFieldValidator, MyCustomValidator>();
```

### **Paso 3: Usar en Config**

```json
{
  "validator": "MY_CUSTOM"
}
```

---

## ?? Referencias

- **Entidad:** `OvexDataModelingTest.Entities.Config.FieldDefinition`
- **Entidad:** `OvexDataModelingTest.Entities.Config.Step_Field`
- **Servicio:** `Onboarding.Core.Services.StepValidationService`
- **Validadores:** `Onboarding.Core.Validators.FieldValidators`
- **Seeder:** `OvexDataModelingTest.Data.OvexDataSeeder`
