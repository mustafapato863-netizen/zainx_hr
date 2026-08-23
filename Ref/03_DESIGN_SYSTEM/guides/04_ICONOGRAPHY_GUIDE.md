# ZainX Iconography Guide

## Base language

Use one coherent line-icon family. Lucide-style geometry is the baseline.

- standard stroke: 1.75px
- compact UI: 14–16px
- standard action: 16–18px
- navigation: 18–20px
- empty/feature illustration: 24–32px only when needed
- do not mix filled and outline icons casually

## Navigation map

| Semantic | Preferred icon | Size | RTL mirror |
|---|---|---:|---|
| Home | House | 18 | No |
| My Work | Inbox / ListChecks | 18 | No |
| People | Users | 18 | No |
| Time | Clock3 | 18 | No |
| Leave | CalendarDays | 18 | No |
| Payroll | WalletCards / ReceiptText | 18 | No |
| Recruitment | BriefcaseBusiness | 18 | No |
| Performance | Target | 18 | No |
| Reports | ChartNoAxesCombined | 18 | No |
| AI | Custom ZainX Intelligence Mark | 18 | No |
| Administration | Settings2 | 18 | No |

## Actions

| Semantic | Icon | RTL mirror |
|---|---|---|
| Add | Plus | No |
| Edit | Pencil | No |
| Delete | Trash2 | No |
| Archive | Archive | No |
| Search | Search | No |
| Filter | SlidersHorizontal | No |
| Advanced filter | ListFilter | No |
| Sort | ArrowUpDown | No |
| Export | Download | No |
| Import | Upload | No |
| Refresh | RefreshCw | No |
| More | Ellipsis | No |
| Copy | Copy | No |
| External open | ExternalLink | Yes only if directional convention requires |
| Back | ArrowLeft | Yes |
| Forward | ArrowRight | Yes |
| Expand | Maximize2 | No |
| Collapse | Minimize2 | No |
| Close | X | No |

## Status

Success → CircleCheck  
Warning → TriangleAlert  
Error → CircleX  
Info → Info  
Pending → Clock  
Locked → Lock  
Draft → FilePenLine  
Finalized → ShieldCheck  
Archived → Archive  
Syncing → RefreshCw  
Offline → CloudOff

## Workforce

Employee → UserRound  
Team → UsersRound  
Department → Network  
Position → Briefcase  
Legal Entity → Building2  
Location → MapPin  
Manager → UserRoundCog  
Contract → FileSignature  
Document → FileText  
Compensation → BadgeDollarSign  
Bank → Landmark  
Attendance → Fingerprint / Clock  
Leave → CalendarRange

## Payroll

Salary → Banknote  
Calculation → Calculator  
Variance → ChartSpline  
Tax → Landmark  
Insurance → Shield  
Payslip → ReceiptText  
Payment → Landmark / Banknote  
Exception → TriangleAlert  
Finalize → ShieldCheck

## Recruitment

Candidate → UserSearch  
Job → BriefcaseBusiness  
Pipeline → Columns3  
Interview → CalendarClock  
Evaluation → ClipboardCheck  
Offer → FileCheck2  
Hire → UserRoundPlus

## Icon color rules

Default functional icon:
secondary text color.

Primary action icon:
inherits button foreground.

Status icon:
semantic tone.

AI:
AI semantic tone only when it communicates AI identity/state.

Do not use bright icon colors merely to decorate navigation.

## Animated icons

Animation is allowed only for:
- Refresh/Sync
- Loading/progress
- AI tool-running
- Success resolve
- brand mark

Do not animate ordinary navigation icons.

## Custom ZainX intelligence icon

Create an SVG derived from ZainX brand geometry when vector brand source is available.

Do not replace it with Sparkles.

Fallback while vector asset is unavailable:
use the supplied ZainX mark as static branding and a restrained generic node/connection icon for tiny system contexts.
