# ReadR - .NET RSS Feed Reader for Azure Integration Learning

> **A hands-on demonstration app for "Harness the Power of Azure in Visual Studio"**

ReadR is a modern RSS feed reader application built with ASP.NET Core Blazor Server that demonstrates progressive Azure integration using Visual Studio's powerful Azure tooling. The app aggregates RSS feeds from the .NET community, including Microsoft blogs, .NET MVPs, and popular YouTube channels.

## 🏗️ Final Architecture

The complete ReadR application consists of:

- **Frontend**: ASP.NET Core Blazor Server app with responsive UI
- **Serverless Functions**: Azure Functions event-driven processing
- **Storage**: Azure Storage Blobs and Queues used for feed processing
- **Orchestration**: Aspire for local development and Azure deployment
- **Monitoring**: Application Insights and Azure Monitor for telemetry and observability
- **Deployment**: Azure Container Apps with Azure Developer CLI (azd) publish support

## 📚 Learning Path - Branch by Branch

This repository demonstrates incremental Azure integration through seven distinct phases. Each branch builds upon the previous, showing how Visual Studio's Azure tools make cloud integration seamless.

### 🎯 Phase 1: Frontend Foundation (`phase1-frontend`)
**Pure .NET web application with no Azure dependencies**

- ✅ ASP.NET Core Blazor Server application
- ✅ RSS feed parsing and aggregation
- ✅ Responsive UI with category navigation
- ✅ File-based feed source configuration
- ✅ In-memory caching

**Visual Studio Features Used:**
- New project templates for Blazor Server
- Built-in debugging and hot reload
- Package management with NuGet
- Right-click deploy to Azure App Service
- GitHub Actions workflows created during publish

### ☁️ Phase 2: Azure Storage Integration (`phase2-storage`)
**Adding cloud storage using Visual Studio's Connected Services**

- ✅ **Connected Services**: Right-click project → Add → Connected Service → Azure Storage
- ✅ Azure Blob Storage for feed configuration
- ✅ Automatic Azure SDK integration
- ✅ Automatic role/permission setup during Connected Services experience

**Visual Studio Features Used:**
- Connected Services wizard for Azure Storage
- Automatic NuGet package installation
- Azure service configuration management
- Connecting projects to live Azure resources

**Key Changes:**
- Added `Azure.Storage.Blobs`, `Azure.Storage.Queues`, `Azure.Data.Tables` packages
- Implemented `AzureBlobFeedSource` replacing file-based configuration
- Integrated Azure Client Factory for service management

### ⚡ Phase 3: Serverless Functions (`phase3-functions`)
**Adding Azure Functions for event-driven background processing**

- ✅ **Azure Functions Project**: Add → New Project → Azure Functions
- ✅ Blob trigger for feed update notifications
- ✅ Queue output binding for message processing
- ✅ Shared services between web app and functions
- ✅ Event-driven architecture patterns
- ✅ Right-click publishing and GitHub Actions workflow creation

**Visual Studio Features Used:**
- Azure Functions project template
- Built-in function triggers and bindings
- Local Functions runtime debugging
- Seamless multi-project solution debugging
- Right-click publish with GitHub Actions workflow generation

**Key Changes:**
- Added `ReadR.Serverless` Functions project
- Implemented `FeedListUpdateTrigger` with blob trigger
- Queue-based messaging between components

### 🎛️ Phase 4: Aspire Orchestration (`phase4-aspire`)
**Local development orchestration with .NET Aspire**

- ✅ **Aspire AppHost**: Right-click solution → Add → New Project → .NET Aspire
- ✅ Multi-project orchestration
- ✅ Service discovery and dependencies
- ✅ Unified debugging experience
- ✅ Development dashboard

**Visual Studio Features Used:**
- Aspire project templates
- Multi-project debugging coordination
- Aspire dashboard integration
- Service dependency visualization

**Key Changes:**
- Added `ReadR.AppHost` and `ReadR.ServiceDefaults` projects
- Configured project references and dependencies
- Simplified development workflow with unified F5 experience

### 🔗 Phase 5: Azure Resource Integration (`phase5-integrations`)
**Connecting Aspire to Azure services**

- ✅ **Aspire Azure Extensions**: Automatic Azure service provisioning
- ✅ Azure Storage emulator integration
- ✅ Service reference management
- ✅ Environment variable orchestration
- ✅ Beginning to transition from Connected Services wire-up to using Aspire integrations

**Visual Studio Features Used:**
- Aspire's Azure hosting extensions
- Automatic service binding and configuration
- Local Azure emulator integration
- Rich IntelliSense for Azure resources

**Key Changes:**
- Integrated Azure Storage with Aspire hosting
- Added service references and wait dependencies
- Configured environment variables for Azure services

### 🚀 Phase 6: Azure Deployment (`phase6-deployment`)
**Infrastructure as Code with Azure Container Apps**

- ✅ **Azure Container Apps**: Aspire's publish command integration
- ✅ Infrastructure as Code (IaC) generation
- ✅ Role-based access control (RBAC) setup
- ✅ Container registry integration
- ✅ Production environment configuration

**Visual Studio Features Used:**
- Right-click AppHost → Publish → Azure Container Apps
- Automatic Bicep template generation
- Azure resource provisioning workflow
- Container deployment pipeline

**Azure Developer CLI (azd) Features Used:**
- Using `azd infra gen` to generate the IAC code to disk for customization

**Key Changes:**
- Added `PublishAsAzureContainerApp()` configuration
- Implemented RBAC with `WithRoleAssignments()`
- Configured Azure Container App Environment

### 📊 Phase 7: Monitoring & Observability (`phase7-monitoring`)
**Application Insights integration for production monitoring**

- ✅ **Application Insights**: Connected Services → Application Insights
- ✅ Distributed tracing across services
- ✅ Performance monitoring
- ✅ Error tracking and alerting
- ✅ Custom telemetry and metrics

**Visual Studio Features Used:**
- Application Insights Connected Service
- Live metrics streaming during debugging
- Exception tracking integration
- Performance profiling tools

**Key Changes:**
- Added Azure Application Insights service
- Integrated telemetry across web app and functions
- Configured monitoring for Container Apps deployment

## 🛠️ Visual Studio Azure Integration Highlights

### Connected Services Workflow
1. **Right-click project** → Add → Connected Service
2. **Select Azure service** (Storage, Key Vault, SQL, etc.)
3. **Automatic package installation** and configuration
4. **Generated code** for service integration
5. **Local development** with emulators

### Aspire Integration Benefits
- **Single F5 deployment** for multi-project solutions
- **Automatic service discovery** and dependency management
- **Real-time dashboard** for monitoring local services
- **Seamless Azure deployment** with infrastructure provisioning

### Azure Deployment from Visual Studio
- **Right-click AppHost** → Publish
- **Select target** (Azure Container Apps, Kubernetes, Docker)
- **Automatic infrastructure** provisioning
- **CI/CD pipeline** integration options

## 🚀 Getting Started

### Prerequisites
- Visual Studio 2022 (v17.12 or later)
- .NET 9.0 SDK
- Azure subscription (for cloud deployment)
- Docker Desktop or Podman (for local containerization)
- Azure Developer CLI (`azd`)
- Azure CLI (`az`)

### Running Locally
1. Clone the repository and switch to desired branch
2. Open `ReadR.slnx` in Visual Studio
3. For Phase 4+ with Aspire: Set `ReadR.AppHost` as startup project
4. Press F5 to run with full orchestration

## 📖 Session Takeaways

After working through these phases, attendees will understand how to:

- **Leverage Connected Services** to rapidly integrate Azure services
- **Use .NET Aspire** for local development and cloud deployment orchestration
- **Right-click deploy** applications to Azure App Service, Functions, and Container Apps
- **CI/CD Pipeline generation** so you can automate your deployments in the future
- **Monitor applications** with Application Insights integration
- **Implement Infrastructure as Code** through Visual Studio workflows
- **Debug multi-service applications** locally with unified tooling

Each phase demonstrates Visual Studio's commitment to making Azure development accessible, productive, and maintainable for .NET developers.

---

*This demo application showcases the seamless integration between Visual Studio and Azure, demonstrating how modern tooling can accelerate cloud-native .NET development.*
