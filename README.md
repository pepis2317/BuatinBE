# BUATIN (Backend)
Backend of Buatin, a milestone based physical products commissioning app that 
covers production, delivery, communication, and seller discovery

### Related Repos

| Part | Repo |
|---|---|
| Admin UI | [Buatin-Admin](https://github.com/vindall/Buatin-Admin) |
| Frontend | [pepis2317/BuatinFE](https://github.com/pepis2317/BuatinFE) |

## What it does
Buatin does not make you pay upfront and hope for the best. Buatin is a marketplace for 
commissioning custom physical products, where payments are split into milestones, funds
are only released when work is confirmed complete, and either party can cancel at any
stage without losing money where they shouldn't.

Every production step requires mutual agreement. When disputes arise, a neutral admin
handles refunds and cancellations so neither party is left hanging.

## Features
- **Location sharing** - for getting the nearest Sellers to the user's location
- **Real-time chat with file attachments** - for keeping clear comms between both parties
- **Midtrans payment gateway** - easily integrate with Indonesian e-wallets and QRis
- **Instagram-like portfolio page with statistics & reviews** - to attract buyers and promote seller
- **Shipment tracking powered by Biteship** - for monitoring delivery of a completed commissioned product
- **Milestone based payments** - ensuring both parties progress together
- **Escrow payments** - ensuring sellers complete their work before getting paid
- **3rd party dispute handling** - refunds and cancellations are processed by admin to esnure neutrality

## Tech Stack
| Layer | Technology |
|---|---|
| Frontend | React Native Expo, TypeScript|
| Backend | .NET Core |
| Database | Microsoft SQL Server |
| Auth | JWT with refresh token |
| Build & Deploy | Dockerize & run on Ubuntu VPS|

### Installation
```bash
git clone https://github.com/pepis2317/BuatinBE.git
cd BuatinBE
dotnet restore
#Config
cp appsettings.example.json appsettings.json
dotnet run
```
## What I'd Improve
- [ ] Clean up handler architecture
- [ ] Improve performance by introducing caching
- [ ] Implement more websockets for certain handlers
