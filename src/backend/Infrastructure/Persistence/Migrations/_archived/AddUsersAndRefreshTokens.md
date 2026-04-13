# Migration: AddUsersAndRefreshTokens

## Tables Created

### AppUsers
| Column       | Type         | Constraints             |
|--------------|--------------|-------------------------|
| Id           | uuid         | PK                      |
| Email        | varchar(256) | NOT NULL, UNIQUE INDEX  |
| FullName     | varchar(200) | NOT NULL                |
| PasswordHash | text         | NOT NULL                |
| Role         | varchar(50)  | NOT NULL                |
| IsActive     | boolean      | NOT NULL, default true  |
| CreatedAt    | timestamptz  | NOT NULL                |

### RefreshTokens
| Column     | Type         | Constraints             |
|------------|--------------|-------------------------|
| Id         | uuid         | PK                      |
| UserId     | uuid         | NOT NULL, FK → AppUsers |
| TokenHash  | varchar(512) | NOT NULL, INDEX         |
| ExpiresAt  | timestamptz  | NOT NULL                |
| CreatedAt  | timestamptz  | NOT NULL                |
| RevokedAt  | timestamptz  | nullable                |

## Run command (from Infrastructure project root)
```
dotnet ef migrations add AddUsersAndRefreshTokens --startup-project ../WebAPI
dotnet ef database update --startup-project ../WebAPI
```