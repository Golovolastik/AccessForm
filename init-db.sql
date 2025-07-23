CREATE TABLE IF NOT EXISTS "RequestTypes" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL
);

INSERT INTO "RequestTypes" ("Id", "Name") VALUES
    (1, 'Заявка на предоставление доступа'),
    (2, 'Уведомление о переводе'),
    (3, 'Заявка на прекращение доступа')
ON CONFLICT ("Id") DO NOTHING;

CREATE TABLE IF NOT EXISTS "AccessRequests" (
    "Id" SERIAL PRIMARY KEY,
    "FullName" VARCHAR(200) NOT NULL,
    "Position" VARCHAR(200) NOT NULL,
    "DocumentPath" TEXT NOT NULL,
    "RequestTypeId" INTEGER NOT NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    "IpAddress" VARCHAR(45),
    CONSTRAINT fk_requesttype FOREIGN KEY("RequestTypeId") REFERENCES "RequestTypes"("Id")
); 