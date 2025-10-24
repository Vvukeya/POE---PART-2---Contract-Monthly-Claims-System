```markdown
# 📋 Contract Monthly Claims System (CMCS)

The **Contract Monthly Claims System (CMCS)** is a web application designed to **streamline monthly claims** for educational institutions. It allows lecturers to submit claims, coordinators to verify them, and managers to approve payments, ensuring **efficiency, accountability, and transparency**.

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

---

## 🛠 Technology Stack

### Backend
- **ASP.NET Core MVC**
- **C#**
- **SQL Server**
- **Microsoft.Data.SqlClient**
- **Session Management**

### Frontend
- **HTML5, CSS3, JavaScript**
- **Bootstrap-inspired responsive design**

### Security
- **Session-based Authentication**
- **Parameterized Queries**
- **File Type Validation**
- **Role-based Access Control**

---

## 📁 Project Structure
```

POE_PART_2___Contract_Monthly_Claims_System/
├── Controllers/
│   ├── HomeController.cs
│   └── DatabaseController.cs
├── Models/
│   ├── User.cs
│   ├── Claim.cs
│   ├── Document.cs
│   └── ErrorViewModel.cs
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
│   │   └── Manager/
│   │       ├── ManagerDashboard.cshtml
│   │       └── ManagerQueue.cshtml
│   └── Shared/
│       ├── _Layout.cshtml
│       └── _ValidationScriptsPartial.cshtml
├── wwwroot/
│   ├── css/Style.css
│   └── uploads/ (document storage)
├── Program.cs
├── appsettings.json
└── POE___PART_2___Contract_Monthly_Claims_System.csproj

````

---

## 🚀 Installation & Setup

### Prerequisites
- .NET 6.0 or 7.0 SDK
- SQL Server (LocalDB or Express)
- Modern web browser

### Steps
1. Clone the project.
2. Open in Visual Studio or IDE.
3. Install dependencies:
```bash
dotnet add package Microsoft.Data.SqlClient
````

4. Update **appsettings.json** connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\claim_system;Database=ContractClaimsDB;Integrated Security=true;"
  }
}
```

5. Initialize database by navigating to `/Database/InitializeDatabase`.
6. Run the application:

```bash
dotnet run
```

7. Access app at `https://localhost:7289` or `http://localhost:5255`.

---

## 👥 User Roles & Access

* **Lecturer:** Submit claims, track status, manage personal claims
* **Coordinator:** Verify claims, provide feedback, department-wide access
* **Manager:** Final approval, financial oversight, institution-wide access

---

## 📊 Database Schema

### Users Table

| Column                    | Description                      |
| ------------------------- | -------------------------------- |
| UserID (PK)               | Unique identifier                |
| Role                      | Lecturer / Coordinator / Manager |
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
| TotalAmount                             | Calculated automatically                               |
| Status                                  | Pending / Verified / Approved / Rejected / Resubmitted |
| SubmissionDate                          | Date submitted                                         |
| CorrectionNotes                         | Coordinator notes                                      |

### Documents Table

| Column                                 | Description        |
| -------------------------------------- | ------------------ |
| DocumentID (PK)                        | Unique document ID |
| ClaimID (FK)                           | Linked claim       |
| FileName, FileType, FilePath, FileSize | File details       |
| UploadDate                             | Date uploaded      |

---

## 🔒 Security Features

* Session-based authentication
* Role-based authorization
* SQL injection prevention
* File validation (type & ≤5MB)
* Client & server input validation
* Session timeout (30 min)
* Anti-forgery tokens

---

## 📱 Responsive Design

Supports:

* Desktop (≥1400px)
* Tablets (768–1024px)
* Mobile (<768px)

---

## 🔄 Workflow

Registration → Login → Claim Submission → Verification → Approval → Tracking → Resubmission

---

## 📄 Supported Files

* PDF (.pdf), Word (.docx), Excel (.xlsx)
* Max 5 MB

---

## 🎨 UI/UX Highlights

* Card-based layout
* Color-coded statuses
* Responsive sortable tables
* Real-time validation
* Loading & error states
* Accessibility support

---

## 🛠 Development Features

* Modular MVC architecture
* Direct SQL integration
* Error logging
* File management
* Session persistence
* Graceful error handling
* Automatic DB initialization

---

## 📈 Performance Optimizations

* Indexed SQL queries
* Efficient resource management
* Streamlined file operations
* Session caching
* Device-optimized layouts

---

## 🔧 Troubleshooting

| Issue                     | Solution                                |
| ------------------------- | --------------------------------------- |
| Database Connection Error | Verify SQL Server and connection string |
| File Upload Fails         | Check type & size (≤5MB)                |
| Login Problems            | Verify email/password, clear cache      |
| Document Viewing Issues   | Confirm upload & browser compatibility  |
| Session Timeout           | Re-login (auto 30 min)                  |

---

## 📞 Support

Contact your **system administrator** or refer to documentation.

---

## 📄 License

Educational project for **POE (Portfolio of Evidence)**.

**Version:** 2.0
**Last Updated:** 24 Oct 2024
**Developed By:** Vutivi Vukeya

```
```
