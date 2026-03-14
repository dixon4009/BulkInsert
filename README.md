# Job Handling API

A robust, enterprise-grade .NET 8 API for asynchronous job processing with multiple execution strategies.

## ✨ Features

- **Multiple Job Processing Strategies**
  - 🔹 **Bulk Processing**: Process all items in one operation
  - 🔹 **Batch Processing**: Process items in configurable batch sizes

- **Robust Architecture**
  - ✅ Clean Architecture (N-Layer)
  - ✅ SOLID Principles
  - ✅ Dependency Injection
  - ✅ Repository Pattern
  - ✅ Strategy Pattern
  - ✅ Factory Pattern

- **Security**
  - 🔐 JWT Authentication
  - 🔐 Bearer Token Authorization
  - 🔐 Role-based Access Control

- **Observability**
  - 📊 Comprehensive Logging (Serilog)
  - 📊 Request/Response Middleware
  - 📊 Real-time Job Status Tracking

- **Testing**
  - ✅ Unit Tests
  - ✅ Integration Tests
  - ✅ BDD Tests (SpecFlow-style)
  - ✅ Mock-based Testing

- **API Documentation**
  - 📖 Swagger/OpenAPI
  - 📖 Auto-generated from Code
  - 📖 Interactive UI

---

## 🔐 Authentication Endpoints

### POST `/api/auth/login`
Authenticates a user and returns a JWT token.

**Authorization:** None (Public endpoint)

**Request Body:**
````````json
{
  "username": "string",
  "password": "string"
}
````````

**Demo Credentials:**
- Username: `admin`
- Password: `password`

**cURL Example:**
````````bash
curl -X POST "https://your-api-url.com/api/auth/login" -H "Content-Type: application/json" -d "{\"username\": \"admin\", \"password\": \"password\"}"
````````


# Response
````````markdown
{
  "token": "your_jwt_token",
  "expiration": "token_expiration_time"
}

````````

## 🛠️ Job Endpoints

### POST `/api/jobs`
Creates a new job with the given data.

**Authorization:** Bearer token required

**Request Body:**
````````json
{
  "jobType": "string",
  "payload": { "key": "value" }
}
````````

**Returns:**
Returns the Job ID (GUID) of the created job.

**Error Responses:**

**400 Bad Request** - Validation error
````````


# Response
````````markdown
{
  "jobId": "new_job_id"
}

````````

### GET `/api/jobs/{id}/status`
Gets the current status of a job.

**Authorization:** Required (Bearer Token)

**Path Parameters:**
- `id` (GUID) - The job identifier

**Success Response (200 OK):**
````````markdown
{
  "jobId": "string",
  "status": "string",
  "progress": 0,
  "workerId": "string",
  "queue": "string",
  "retryCount": 0
}
````````

**Error Responses:**

**401 Unauthorized** - Invalid or missing token
**404 Not Found** - Job does not exist

````````

### GET `/api/jobs/{id}/logs`
Gets the processing logs of a job.

**Authorization:** Required (Bearer Token)

**Path Parameters:**
- `id` (GUID) - The job identifier

**Success Response (200 OK):**
````````markdown
{
  "jobId": "string",
  "logs": [
    {
      "timestamp": "log_timestamp",
      "message": "log_message",
      "level": "log_level"
    }
  ]
}
````````

**Error Responses:**

**401 Unauthorized** - Invalid or missing token
**404 Not Found** - Job or logs do not exist

---

## 📖 Complete API Reference

### Endpoint Summary

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/login` | ❌ No | Authenticate and get JWT token |
| POST | `/api/jobs` | ✅ Yes | Start a new job |
| GET | `/api/jobs/{id}/status` | ✅ Yes | Get job status |
| GET | `/api/jobs/{id}/logs` | ✅ Yes | Get job processing logs |
