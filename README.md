# ReadR - .NET RSS Feed Reader

ReadR is a modern RSS feed reader application built with ASP.NET Core Blazor Server that aggregates RSS feeds from the .NET community, including Microsoft blogs, .NET MVPs, and popular YouTube channels. The application provides a clean, responsive interface for browsing and discovering .NET-related content from across the web.

## 🏗️ Architecture

ReadR is built as a cloud-native application with the following components:

- **Frontend**: ASP.NET Core Blazor Server app with responsive UI
- **Storage**: Azure Storage Blobs and Queues for feed data and processing
- **Orchestration**: .NET Aspire for local development and Azure deployment
- **Monitoring**: Application Insights and Azure Monitor for telemetry and observability
- **Deployment**: Azure Container Apps with Azure Developer CLI (azd) support

## ✨ Features

- **RSS Feed Aggregation**: Automatically fetches and parses RSS feeds from various .NET community sources
- **Responsive UI**: Clean, modern interface that works across desktop and mobile devices
- **Category Navigation**: Organize feeds by source type (Microsoft blogs, MVP blogs, YouTube channels, etc.)
- **Real-time Updates**: Background processing keeps feed content fresh
- **Cloud Storage**: Feed configurations and data stored in Azure Storage
- **Monitoring**: Built-in Application Insights integration for performance tracking

## 🛠️ Technology Stack

- **Frontend**: ASP.NET Core 9.0, Blazor Server, Bootstrap CSS
- **Storage**: Azure Blob Storage, Azure Storage Queues
- **Orchestration**: .NET Aspire
- **Monitoring**: Azure Application Insights
- **Deployment**: Azure Container Apps
- **Infrastructure**: Bicep templates

## � Getting Started

### Prerequisites
- Visual Studio 2022 (v17.12 or later) or Visual Studio Code
- .NET 9.0 SDK
- Azure subscription (for cloud deployment)
- Docker Desktop or Podman (for local containerization)
- Azure Developer CLI (`azd`)
- Azure CLI (`az`)

### Running Locally

1. **Clone the repository**
   ```bash
   git clone https://github.com/bradygaster/ReadR.git
   cd ReadR
   ```

2. **Open in Visual Studio**
   ```bash
   ReadR.slnx
   ```

3. **Run with Aspire orchestration**
   - Set `ReadR.AppHost` as the startup project
   - Press F5 to run the complete application stack

### Deployment to Azure

1. **Deploy using Azure Developer CLI**
   ```bash
   azd up
   ```

2. **Alternative: Deploy from Visual Studio**
   - Right-click `ReadR.AppHost` project
   - Select "Publish"
   - Choose "Azure Container Apps"
   - Follow the deployment wizard

## 📁 Project Structure

- **ReadR.Frontend**: Main Blazor Server web application
- **ReadR.AppHost**: .NET Aspire orchestration and deployment configuration
- **ReadR.ServiceDefaults**: Shared service configurations and extensions

## � Configuration

The application uses Azure Blob Storage for feed source configuration. Feed URLs are managed in the `feed-urls.txt` file, which can be updated through Azure Storage or during deployment.

## 📊 Monitoring

ReadR includes comprehensive monitoring through Azure Application Insights:
- Request tracking and performance metrics
- Exception logging and error tracking
- Custom telemetry for feed processing
- Real-time metrics dashboard

## 🤝 Contributing

Contributions are welcome! Please feel free to submit issues, feature requests, or pull requests to help improve ReadR.
