# 📋 Contract Monthly Claims System (CMCS)

The **Contract Monthly Claims System (CMCS)** is a web application designed to **streamline monthly claims** for educational institutions. It allows lecturers to submit claims, coordinators to verify them, managers to approve them, and HR to process payments—ensuring **efficiency, accountability, and transparency**.

---

## 🎯 Key Features

### 👨‍🏫 Lecturer Features
- **Dashboard:** Claim statistics and recent submissions  
- **Submit Claims:** Upload claims with supporting documents  
- **Track Claims:** Monitor claim status  
- **Edit & Resubmit:** Modify rejected claims  
- **Document Management:** Manage PDFs, DOCX, XLSX files  

### 👨‍💼 Coordinator Features
- **Verification Queue:** Review pending claims  
- **Document Review:** Access uploaded files  
- **Approve/Reject:** Provide detailed feedback  
- **Analytics:** Track verification statistics  

### 👨‍💻 Manager Features
- **Approval Queue:** Final claim approval  
- **Oversight:** Monitor all claim activity  
- **Financial Overview:** Track approved claims  
- **Quality Control:** Ensure policy compliance  

### 💼 HR Manager Features (NEW)
- **HR Dashboard:** Payment workflow overview  
- **Lecturer Management:** Edit lecturer details  
- **Payment Processing:** Pay approved claims  
- **Invoice Generation:** Create PDF invoices  
- **Reports:** Generate financial reports  
- **Claim Tracking:** View claim payment statuses  

---

## 🛠 Technology Stack

### Backend
- **ASP.NET Core MVC**  
- **C#**  
- **SQL Server / LocalDB**  
- **Microsoft.Data.SqlClient**  
- **Session Management**

### Frontend
- **HTML5, CSS3, JavaScript**  
- **Bootstrap-inspired responsive design**

### Security
- **Session-based Authentication**  
- **Parameterized SQL Queries**  
- **File Type Validation**  
- **Role-based Access Control (RBAC)**  

---

## 📁 Project Structure

```
POE_PART_2___Contract_Monthly_Claims_System/
├── Controllers/
│   ├── HomeController.cs
│   ├── HRController.cs
│   └── DatabaseController.cs
├── Models/
│   ├── User.cs
│   ├── Claim.cs
│   ├── Document.cs
│   ├── ErrorViewModel.cs
│   └── LecturerInvoice.cs
├── Data/
│   ├── DatabaseConnection.cs
│   └── AllQueries.cs
├── Views/
│   ├── Home/
│   │   ├── Index.cshtml
│   │   ├── Login.cshtml
│   │   ├── Register.cshtml
│   │   ├── Error.cshtml
│   │   ├── Lecturer/
│   │   │   ├── LecturerDashboard.cshtml
│   │   │   ├── LecturerSubmitClaim.cshtml
│   │   │   ├── LecturerTrack.cshtml
│   │   │   └── LecturerEditClaim.cshtml
│   │   ├── Coordinator/
│   │   │   ├── CoordinatorDashboard.cshtml
│   │   │   └── CoordinatorQueue.cshtml
│   │   ├── Manager/
│   │   │   ├── ManagerDashboard.cshtml
│   │   │   └── ManagerQueue.cshtml
│   ├── HR/
│   │   ├── HRDashboard.cshtml
│   │   ├── ManageLecturers.cshtml
│   │   ├── EditLecturer.cshtml
│   │   ├── GenerateReports.cshtml
│   │   ├── GenerateInvoices.cshtml
│   │   ├── ViewClaimDetails.cshtml
│   │   └── ViewClaimStatuses.cshtml
│   └── Shared/
│       ├── _Layout.cshtml
│       └── _ValidationScriptsPartial.cshtml
├── wwwroot/
│   ├── css/Style.css
│   ├── images/
│   └── uploads/
├── Program.cs
├── appsettings.json
└── POE___PART_2___Contract_Monthly_Claims_System.csproj
```

---

## 🚀 Installation & Setup

### Prerequisites
- .NET 6.0 or 7.0 SDK  
- SQL Server (LocalDB or Express)  
- Modern web browser  

### Steps
1. Clone the project.  
2. Open in Visual Studio or preferred IDE.  
3. Install dependencies:

```bash
dotnet add package Microsoft.Data.SqlClient
```

4. Update **appsettings.json**:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\claim_system;Database=ContractClaimsDB;Integrated Security=true;"
  }
}
```

5. Run the application:

```bash
dotnet run
```

6. Access the app at:  
`https://localhost:7289`  
or  
`http://localhost:5255`

---

## 👥 User Roles & Access

- **Lecturer:** Submit & track claims  
- **Coordinator:** Verify & review claims  
- **Manager:** Approve claims  
- **HR Manager:** Process payments, generate invoices & reports  

---

## 📊 Database Schema

### Users Table
| Column                    | Description                      |
| ------------------------- | -------------------------------- |
| UserID (PK)               | Unique identifier                |
| Role                      | Lecturer / Coordinator / Manager / HR |
| FirstName, Surname, Email | Personal info                    |
| Address, PhoneNumber      | Contact info                     |
| RegistrationDate          | Account creation date            |
| Password                  | Secure password                  |

### Claims Table
| Column                                  | Description                                            |
| --------------------------------------- | ------------------------------------------------------ |
| ClaimID (PK)                            | Unique claim ID                                        |
| UserID (FK)                             | Linked user                                            |
| Month, Year                             | Claim period                                           |
| ModuleCode, Sessions, Hours, HourlyRate | Work details                                           |
| TotalAmount                             | Auto-calculated                                        |
| Status                                  | Pending / Verified / Approved / Rejected / Resubmitted / Paid |
| SubmissionDate                          | Date submitted                                         |
| CorrectionNotes                         | Notes from coordinators                                |
| PaymentDate                             | When HR processed payment                              |
| PaidBy                                  | HR member who paid                                     |

### Documents Table
| Column                                 | Description        |
| -------------------------------------- | ------------------ |
| DocumentID (PK)                        | Unique document ID |
| ClaimID (FK)                           | Linked claim       |
| FileName, FileType, FilePath, FileSize | File details       |
| UploadDate                             | Date uploaded      |

---

## 🔒 Security Features

- Session-based authentication  
- Role-based authorization  
- SQL injection prevention  
- File validation (type & ≤5MB)  
- Anti-forgery tokens  
- Session timeout (30 min)  

---

## 📱 Responsive Design

Supports:
- Desktop  
- Tablets  
- Mobile  

---

## 🔄 Workflow

Registration → Login → Claim Submission → Verification → Approval → Payment (HR) → Tracking

---

## 📄 Supported Files

- PDF (.pdf)  
- Word (.docx)  
- Excel (.xlsx)  
- Max size: 5MB  

---

## 🎨 UI/UX Highlights

- Card-based layout  
- Modern gradients  
- Color-coded statuses  
- Sortable tables  
- Real-time validation  
- Smooth navigation  

---

## 🛠 Development Features

- MVC architecture  
- Direct SQL & parameterized queries  
- Error logging  
- Secure file handling  
- Session persistence  
- Automatic DB initialization  

---

## 📈 Performance Optimizations

- Indexed SQL queries  
- Efficient I/O operations  
- Session caching  
- Optimized database schema  

---

## 🔧 Troubleshooting

| Issue                     | Solution                                |
| ------------------------- | ---------------------------------------- |
| Database Connection Error | Check SQL Server + connection string     |
| File Upload Fails         | Ensure file type & size (≤5MB)           |
| Login Problems            | Verify credentials, clear browser cache  |
| Document Viewing Issues   | Check uploads folder & permissions       |
| Session Timeout           | Re-login (30 minutes)                    |

---

## 📞 Support

Contact your **system administrator** or refer to documentation.

---

## 📄 License

Educational project for **POE (Portfolio of Evidence)**.  
**Version:** 2.0  
**Last Updated:** 20 November 2024  
**Developed By:** *Vutivi Vukeya*
