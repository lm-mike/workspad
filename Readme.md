# Project requirements

* Requires .Net Core 6 ver. to be installed
* Access to https://artifactory.astralinux.ru
* Connection to WorksPad server.


# Check if .Net Core is already installed
```bash
dotnet --list-sdks #check if .Net SDK is already installed
dotnet --list-runtimes #check it .Net runtimes is already installed
```

# Install .Net Core

Please use Astra-Linux [documentation](https://wiki.astralinux.ru/pages/viewpage.action?pageId=41192241).


# appsettings.ENVIRONMENTNAME.json

## MyChatBotConfiguration

|Name|Description|
|---|---|
|`ChatBotUrl`|ChatBot endpoint|
|`ServerUrl`|ChatBotAPI service endpoint|
|`ChatBotToken`|Token generated and provided by WorksPAd Admin|
|`ServerToken`|Token generated and provided by WorksPAd Admin|

## APIConfig

|Name|Description|
|---|---|
|`BaseAddress`|Jira base address|
|`ldapAddress`|Ldap endpoint|
|`ldapPort`|Port listened by LdapServer|
|`ldapUser`|Ldap Service Account|
|`ldapPassword`|Ldap Service Account password|

---
# Deployment bash commands
```bash
dotnet nuget list source #check if private .nupkg source already exists by getting a list of sources
dotnet nuget add source <$SOURCEPATH> -n <$SOURCENAME> #if source doesn't exist run this command
dotnet restore # next command is used to restore project dependencies
dotnet dev-certs https # to be discussed
dotnet build --configuration $ENV_VAR --output $OUTPUT_DIRECTORY
cd $OUTPUT_DIRECTORY
export ASPNETCORE_ENVIRONMENT=$ENV_VAR
dotnet SearchPRBot.dll
```

# appsettings.json example
```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://*:7094"
      }
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "MyChatBotConfiguration": {
    "ChatBotUrl": "https://bots.astra.local/lifehelperbot",
    "ServerUrl": "https://workspad.astra.local/api",
    "ChatBotToken": "bot_token",
    "ServerToken": "server_tolen"
  },
  "APIConfig": {
    "BaseAddress": "https://jira.astra.local/rest",
    "ldapAddress": "10.0.0.101",
    "ldapPort": 389,
    "ldapUser":"sysaccount_name",
    "ldapPassword": "sysaccount_password"
  },
  "DefaultValues": {
    "tmpPersPassDefaults": {
      "appendPath": "servicedeskapi/request",
      "fields": {
        "serviceDeskId": "22",
        "requestTypeId": "177",
        "summary": "Выдача временного пропуска",
        "description": "Просьба выдать персональный пропуск посетителю. ФИО посетителя: {0}. Дата посещения: {1}"
      }
    },
    "carPassDefaults": {
      "appendPath": "api/2/issue",
      "businessCenters": {
        "WarsawPlaza": "БЦ Варшавская Плаза",
        "Wave": "БЦ Волна",
        "SKY": "БЦ SKY",
        "Capital": "БЦ Капитал"
      },
      "fields": {
        "project": "CORP",
        "issuetype": "Пропуск",
        "summary": "Заказ пропуска на машину"
      }
    },
    "vacationStatementDefaults": {
      "appendPath": "api/2/issue",
      "vacationtype": {
        "Yearly": "Ежегодный оплачиваемый",
        "SelfPaid": "За свой счет"
      },
      "employers": {
        "Consulting": "ООО 'Астра-Консалтинг'",
        "iclastraserv": "ООО 'АйСиЭл Астра Сервис'",
        "ispsystem": "ООО 'ISPsystem'",
        "rubackup": "ООО 'РУБЭКАП'",
        "rupost": "ООО 'РуПост'",
        "rusbitech": "ООО 'РусБИТех-Астра'",
        "tantorlabs": "ООО 'Тантор Лабс'",
        "uveon": "ООО 'УВЕОН'",
        "gruppaastra": "АО \"Группа Астра\"",
        "resolut": "ООО «РЕСОЛЮТ»"
      },
      "fields": {
        "project": "CORP",
        "issuetype": "Отпуск",
        "summary": "Заявление на отпуск"
      }
    },
    "vegaSupportDefaults": {
      "appendPath": "servicedeskapi/request",
      "fields": {
        "portalid": "22",
        "issuetype": {
          "Inc": "176",
          "Request": "177"
        }
      }
    },
    "responsibleManagerDefaults":{
      "appendPath": "insight/1.0/iql/objects",
      "companyChemaId" : 4,
      "managerSchemaId": 9,
      "companyNameIql" : "Наименование LIKE \"{0}\"",
      "companyIdIql": "Key IN (\"{0}\")",
      "managerIql": "key = {0}"
    }
  }
}

```

---