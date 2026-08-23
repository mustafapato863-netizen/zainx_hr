# Backend → Frontend Experience Module Map

| Frontend package | Primary backend contracts consumed | Notes |
|---|---|---|
| `people` | People, Organization, Documents | Employee profile stays one UX workspace; backend ownership remains separate |
| `attendance` | Attendance, People snapshot/read contracts, Integrations for device/import status | Never query People tables |
| `leave` | Leave, People read contracts, Approvals | Attendance can consume approved leave via backend contracts |
| `payroll` | Payroll, Compliance, Settlement, Attendance/Leave approved impact, Integrations exports | Frontend never owns calculation logic |
| `recruitment` | Recruitment, Documents, Approvals, People hire-conversion command contract | Candidate is not Employee until backend conversion |
| `approvals` | Approvals + purpose-built projections from source modules | Universal My Work inbox |
| `reports` | Reporting read models | Do not reconstruct reports by client-side joining many APIs |
| `administration` | Tenancy, Identity, Organization config, Integrations, Notifications, Audit, System operations | Admin UX is broader than one backend schema |
| `ai` | AI module/tool contracts + approved read tools from business modules | No direct DB/GraphQL arbitrary write |

## Rule

If a frontend screen needs cross-module composition, request a purpose-built backend read contract or approved GraphQL composite read. Do not make the browser an integration layer.
