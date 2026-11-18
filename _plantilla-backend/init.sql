-- init.sql
-- Esquema mínimo para la plantilla (login + preguntas de seguridad)
-- Ejecutar sobre una BD PostgreSQL nueva: psql -d plantilla_db -f init.sql

CREATE SCHEMA IF NOT EXISTS "PlantillaBackend";

-- 1. Roles
CREATE TABLE IF NOT EXISTS "PlantillaBackend".roles (
    id_rol SERIAL PRIMARY KEY,
    sdescripcion VARCHAR(100) NOT NULL,
    benabled BOOLEAN NOT NULL DEFAULT TRUE,
    nusuario_creacion INTEGER NOT NULL DEFAULT 1,
    dfecha_creacion TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    nusuario_actualizacion INTEGER,
    dfecha_actualizacion TIMESTAMP
);

-- 2. Funcionarios
CREATE TABLE IF NOT EXISTS "PlantillaBackend".funcionarios (
    id_funcionario SERIAL PRIMARY KEY,
    snacionalidad VARCHAR(1) NOT NULL DEFAULT 'V',
    ncedula_ident INTEGER NOT NULL,
    sprimer_apellido VARCHAR(100) NOT NULL,
    ssegundo_apellido VARCHAR(100),
    sprimer_nombre VARCHAR(100) NOT NULL,
    ssegundo_nombre VARCHAR(100),
    spassword_hash VARCHAR(500) NOT NULL,
    rol_id INTEGER NOT NULL,
    benabled BOOLEAN NOT NULL DEFAULT TRUE,
    nusuario_creacion INTEGER NOT NULL DEFAULT 1,
    dfecha_creacion TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    nusuario_actualizacion INTEGER,
    dfecha_actualizacion TIMESTAMP,
    CONSTRAINT funcionarios_roles FOREIGN KEY (rol_id)
        REFERENCES "PlantillaBackend".roles(id_rol) ON DELETE RESTRICT,
    CONSTRAINT uq_funcionario_cedula UNIQUE (snacionalidad, ncedula_ident)
);

-- 3. Preguntas de seguridad (catálogo maestro)
CREATE TABLE IF NOT EXISTS "PlantillaBackend".preguntas_seg (
    id_preguntaseg SERIAL PRIMARY KEY,
    sdescripcion VARCHAR(250) NOT NULL,
    benabled BOOLEAN NOT NULL DEFAULT TRUE,
    nusuario_creacion INTEGER NOT NULL DEFAULT 1,
    dfecha_creacion TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    nusuario_actualizacion INTEGER,
    dfecha_actualizacion TIMESTAMP
);

-- 4. Preguntas asignadas a cada funcionario
CREATE TABLE IF NOT EXISTS "PlantillaBackend".funcionario_pseguridad (
    id_fpseguridad SERIAL PRIMARY KEY,
    id_funcionario INTEGER NOT NULL,
    id_preguntaseg INTEGER NOT NULL,
    dfecha_reg TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    srespuesta VARCHAR(250) NOT NULL,
    nusuario_creacion INTEGER NOT NULL DEFAULT 1,
    dfecha_creacion TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    nusuario_actualizacion INTEGER,
    dfecha_actualizacion TIMESTAMP,
    CONSTRAINT funcionario_pseguridad_funcionario_fkey FOREIGN KEY (id_funcionario)
        REFERENCES "PlantillaBackend".funcionarios(id_funcionario) ON DELETE CASCADE,
    CONSTRAINT funcionario_pseguridad_preguntas_seg_fkey FOREIGN KEY (id_preguntaseg)
        REFERENCES "PlantillaBackend".preguntas_seg(id_preguntaseg) ON DELETE RESTRICT,
    CONSTRAINT uq_funcionario_pregunta UNIQUE (id_funcionario, id_preguntaseg)
);

-- Roles base
INSERT INTO "PlantillaBackend".roles (sdescripcion)
SELECT seed.sdescripcion
FROM (
    VALUES ('Administrador'), ('Operador'), ('Supervisor')
) AS seed(sdescripcion)
WHERE NOT EXISTS (
    SELECT 1
    FROM "PlantillaBackend".roles r
    WHERE r.sdescripcion = seed.sdescripcion
);

-- Preguntas de seguridad base
INSERT INTO "PlantillaBackend".preguntas_seg (sdescripcion)
SELECT seed.sdescripcion
FROM (
    VALUES
        ('¿Cuál es el nombre de tu primera mascota?'),
        ('¿En qué ciudad naciste?'),
        ('¿Cuál fue tu primer empleo?')
) AS seed(sdescripcion)
WHERE NOT EXISTS (
    SELECT 1
    FROM "PlantillaBackend".preguntas_seg p
    WHERE p.sdescripcion = seed.sdescripcion
);