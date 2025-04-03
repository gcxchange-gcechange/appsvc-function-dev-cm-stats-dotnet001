# Career Marketplace Statistics

## Summary

Provides functionality for storing and retrieving stats from Google Analytics (GA)
- Store GA report in storage container
- Retrieve data from storage container

## Prerequisites

n/a

## Version 

![dotnet 8](https://img.shields.io/badge/net8.0-blue.svg)

## API permission

MSGraph

| API / Permissions name    | Type        | Admin consent | Justification                       |
| ------------------------- | ----------- | ------------- | ----------------------------------- |
| Sites.Read.All            | Delegasted  | Yes           | Read opportunity list               |   
| User.Read                 | Delegated   | Yes           | Sign in and read user profile       | 

Sharepoint

n/a

## App setting

| Name                        | Description                                                                    |
| --------------------------- | ------------------------------------------------------------------------------ |
| AzureWebJobsStorage         | Connection string for the storage acoount                                      |
| clientId                    | Id of the app registration used to authenticate user                           |
| containerName               | The name of the storage container that contains the GA data                    |
| delegatedUserName           | Name of the user for delegated access                                          |
| delegatedUserSecret         | Name of the secret for delegated access                                        |
| keyVaultUrl                 | Address of the key vault                                                       |
| listId                      | Id of the job opportunity list                                                 |
| privateKeySecretName        | Name of the secret used to store the private api key for Google                |
| propertyId                  | Id of the Google Analytics property                                            |
| secretName                  | Name of the secret used for authentication                                     |
| siteId                      | Id of the site that hosts the job opportunity list                             |
| skillsNameFr                | Name of the column for skills name French (use as required)                    |
| tenantId                    | Id of the SharePoint tenant                                                    |
| type                        | Credential for Google Analytics client                                         |
| project_id                  | Credential for Google Analytics client                                         |
| private_key_id              | Credential for Google Analytics client                                         |
| client_email                | Credential for Google Analytics client                                         |
| client_id                   | Credential for Google Analytics client                                         |
| auth_uri                    | Credential for Google Analytics client                                         |
| token_uri                   | Credential for Google Analytics client                                         |
| auth_provider_x509_cert_url | Credential for Google Analytics client                                         |
| client_x509_cert_url        | Credential for Google Analytics client                                         |
| universe_domain             | Credential for Google Analytics client                                         |

## Version history

Version|Date|Comments
-------|----|--------
1.0|TBD|Initial release

## Disclaimer

**THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.**
