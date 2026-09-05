-- Creates one database per microservice (database-per-service pattern).
-- Runs automatically the first time the postgres container starts
-- (mounted into /docker-entrypoint-initdb.d).

CREATE DATABASE identity_db;
CREATE DATABASE catalog_db;
CREATE DATABASE order_db;
CREATE DATABASE payment_db;
