# DFW Azure UG: Microservices in Azure
## Presented 18 July 2017

This code powers the demonstration of a distributed photo upload handling service. That demo makes use of various Azure services that support microservices architecture, including:

* App Service
  * Web Apps
  * Logic Apps
  * API Apps
  * Function Apps
* Azure SQL Database
* Application Insights
* Storage
* CDN
* Traffic Manager
* Service Bus

It also makes use of an IaaS-based virtual machine, which runs as an FTP server and sends files to Azure storage and messages to a Service Bus queue, to show how you can incorporate traditional services into microservice architectures.
