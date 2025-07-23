DROP TABLE IF EXISTS "AccessRequests" CASCADE;

CREATE TABLE "AccessRequests" (
    "Id" SERIAL PRIMARY KEY,
    "FullName" VARCHAR(200) NOT NULL,
    "Position" VARCHAR(200) NOT NULL,
    "DocumentPath" TEXT NOT NULL,
    "RequestTypeId" INTEGER NOT NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    "IpAddress" VARCHAR(45),
    CONSTRAINT fk_requesttype FOREIGN KEY("RequestTypeId") REFERENCES "RequestTypes"("Id")
); 