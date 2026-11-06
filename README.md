# CCD
CCD-assignment for Inholland + Hypersolid

## Prerequisites
Docker

## Local setup

### Step 1:
 Clone the repository

### Step 2:
Restore dependencies:
```bash
dotnet restore
```

### Step 3:
Start azurite with docker:
```bash
docker run -d --name azurite -p 10000:10000  -p 10001:10001  -p 10002:10002 mcr.microsoft.com/azure-storage/azurite:latest
```

### Step 4:
Run the project:
``` bash
cd WeatherFunctions
func start
```

### Step 5:
Make a request.
```bash
Default request url:  http://localhost:7071/api/images
```

## Endpoints
```
Functions:

        GetImageJobStatus: [GET] {url}/api/images/{jobId}/status

        GetJobResults: [GET] {url}/api/images/{jobId}

        StartImageJob: [POST] {url}/api/images

        ProcessImageJob: queueTrigger

        ProcessWeatherJob: queueTrigger
```