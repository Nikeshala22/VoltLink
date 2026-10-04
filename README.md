# VoltLink (Smart Solar Microgrid Trading System)

## SE4040 - Enterprise Application Development

### Assignment 

**Project Title:** Smart Solar Microgrid Trading System - Client-Server Application

**Technologies:**
- Web Application
- Pure Native Android Mobile Application
- C# Web API
- MongoDB
- SQLite
- Google Maps API
- QR Code
- IIS Server

---

## 1. Project Description

The Smart Solar Microgrid Trading System is an end-to-end client-server application developed to manage solar energy trading between solar prosumers, microgrid nodes, backoffice users, and grid operators.

The system consists of:

- Web Application
- Native Android Mobile Application
- Centralized C# Web Service
- MongoDB NoSQL Database

Both the Web Application and Android Mobile Application communicate with the centralized Web Service through RESTful API calls.

The Web Application and Mobile Application do not directly access the MongoDB database. Business logic is handled by the central Web Service using the FAT Service architecture.

---
## GitHub Repository
https://github.com/Nikeshala22/VoltLink


## Individual Contributions

| Member               | IT Number      | Contribution                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| -------------------- | -------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Madawalage N S**   | **IT23307308** | **System Architecture & Node Management** – Designed the client–server architecture, layered Web API structure (Controllers, Services, Repositories), and FAT service approach. **Microgrid Node Management** **Web Service:** Implemented node creation and updating with GPS location, capacity, and battery slots; schedule/booking-window management; node activation and deactivation, including preventing deactivation when active reservations exist; and nearby-node queries. **Web App:** Developed the Nodes page and Node Details page, including schedules and battery-slot information. **Mobile App:** Developed nearby-node listing, Google Maps integration, and device-location functionality. |
| **Ama B L N S**      | **IT23211414** | **Energy Slot Reservation Management** – **Web Service:** Implemented creation, updating, and cancellation of reservations; 7-day booking and 12-hour notice rules; reservation approval and rejection; QR-code generation upon approval; and booking search functionality. **Web App:** Developed the Reservations page with filters and the Reservation Details page for approving or rejecting bookings. **Mobile App:** Developed booking creation, modification, and cancellation; booking summary screens; booking history and pending bookings; booking search; and QR-code display.                                                                                        |
| **M D L Perera**     | **IT23255524** | **User Management & Energy Transfer** – **Web Service:** Implemented JWT-based authentication, role-based access control for Backoffice and Grid Operator roles, staff-user creation, updating, activation and deactivation, password hashing, QR-code verification, and completion of energy transfers. **Web App:** Developed the Sign-in page, system-user management page, and role-based navigation. **Mobile App:** Developed login and role-based home screens, SQLite local user-data storage, and Operator Mode for scanning QR codes, verifying them with the server, and completing energy transfers.                                                                   |
| **M U D Gunatilake** | **IT23215924** | **Prosumer Management** – **Web Service:** Implemented prosumer registration, creation, updating, and deactivation using NIC as the unique key; prosumer deactivation requests; Backoffice-only reactivation; pending-activation listing; and dashboard endpoints. **Web App:** Developed the Prosumers page, Pending Activations page, and Dashboard page. **Mobile App:** Developed the splash screen, prosumer registration using NIC, profile editing, deactivation requests, and the home dashboard displaying active and pending counts.                                                                                                                                     |


---

##  Video Demonstration

A video demonstration of the application is provided below.

**Video Link:**  
https://mysliit-my.sharepoint.com/:f:/g/personal/it23307308_my_sliit_lk/IgAFe8whosw0QY7LY7acWPx7AR6firJx18q5PKb1b2ZYcdM?e=J0cXpo
