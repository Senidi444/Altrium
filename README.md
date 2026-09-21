# Altrium Recruitment System

A full-stack recruitment management portal developed using ASP.NET Core MVC, Entity Framework Core, and SQL Server.

The system supports multiple user roles and provides an end-to-end recruitment workflow for applicants, recruiters, administrators, and management.

## Technologies Used

- ASP.NET Core MVC
- C#
- Entity Framework Core
- SQL Server / SQL Server Express
- Razor Views
- HTML5
- CSS3
- JavaScript
- BCrypt password hashing
- Cookie-based authentication
- SMTP email notifications
- Google Meet links for online interviews

## User Roles

The system supports four main user roles:

### Applicant

Applicants can:

- Register and log in
- Complete their profile
- Add education and experience
- Upload a CV
- Browse available vacancies
- Search and filter vacancies
- Apply for vacancies
- View submitted applications
- Track application status
- Receive in-app notifications
- Receive email notifications
- View scheduled interviews
- Access Google Meet interview links
- View interview outcomes and interviewer feedback

### Recruiter

Recruiters can:

- Log in after approval
- Create and manage vacancies
- Close and reopen vacancies
- View applicants
- View applicant profiles and CV information
- Shortlist applicants
- Reject applicants
- Schedule interviews
- Provide interview details and messages
- Add Google Meet links
- Submit interview outcomes and feedback
- Send applicant notifications and emails

### Administrator

Administrators can:

- View system statistics
- Manage users
- Search users
- Activate and deactivate accounts
- Approve recruiters
- Manage recruiter access

### Management

Management users can:

- View recruitment statistics
- Monitor vacancies and applications
- View shortlisted and rejected applications
- View interview statistics
- Access management reporting information

## Key Features

### Authentication and Authorisation

- User registration and login
- Role-based access control
- Protected controller actions
- Applicant, Recruiter, Admin and Management roles
- Account activation/deactivation
- Recruiter approval workflow

### Applicant Management

- Applicant profile management
- Education records
- Experience records
- CV upload and management
- Profile completeness validation

### Vacancy Management

- Vacancy creation
- Vacancy browsing
- Vacancy searching and filtering
- Vacancy status management
- Vacancy closing and reopening
- Vacancy deletion with application-history protection

### Application Management

- Vacancy applications
- Duplicate application prevention
- Profile-completion validation before applying
- Application status tracking
- Shortlisting
- Rejection
- Applicant notifications

### Interview Management

- Interview scheduling
- Interview date and time
- Interview stage and type
- Interview message
- Google Meet link
- Applicant interview notifications
- Interview outcome recording
- Interviewer feedback
- Completed interview status

> Google Meet support is implemented using valid Google Meet links provided by the recruiter. The system validates and stores the link and provides it to the applicant.

### Notifications

The system provides in-app notifications for:

- Application status updates
- Interview invitations
- Interview updates
- General system messages

Applicants can view their notifications and mark them as read.

### Email Notifications

SMTP email notifications are supported for:

- Application shortlisting
- Application rejection
- Interview scheduling
- Interview feedback

Email configuration should be provided locally and must not be committed to the public repository.

### Management Reporting

Management users can access recruitment statistics including:

- Total vacancies
- Total applications
- Applications by status
- Shortlisted applications
- Rejected applications
- Interview statistics

## Security

The system includes:

- BCrypt password hashing
- Cookie-based authentication
- Role-based authorisation
- Recruiter approval
- Account activation/deactivation
- Anti-forgery protection for forms
- Validation of user input
- Duplicate application prevention
- Protected CV upload handling
- Sensitive email credentials excluded from the public repository

## Prerequisites

Before running the system, install:

- Visual Studio 2022
- ASP.NET and web development workload
- SQL Server Express or another SQL Server instance
- SQL Server Management Studio (optional)

## Setup Instructions

### 1. Clone the Repository

Clone the repository:

```text
https://github.com/Senidi444/Altrium.git