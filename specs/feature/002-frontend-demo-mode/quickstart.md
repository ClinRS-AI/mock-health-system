# Quickstart: Experiencing Demo Mode Locally

**Date**: 2026-06-07 | **Plan**: [plan.md](plan.md)

This guide explains how to see demo mode in action during local development.

## Prerequisites

- Backend configured with a PostgreSQL database (see `CLAUDE.md` for setup)
- `AUTH_SETTINGS_ADMIN_KEY` set to any non-empty value in `backend/.env`

## Steps: Demo Mode Active (Admin Key Configured)

1. **Start the backend**

   ```bash
   cd backend
   dotnet run --project MockHealthSystem.Api
   # Starts on http://localhost:5001
   ```

2. **Start the frontend**

   ```bash
   cd frontend
   npm run dev
   # Opens on http://localhost:5176
   ```

3. **Open the app without signing in**

   Navigate to `http://localhost:5176` in your browser. Do not enter any admin key.

4. **View demo mode**

   Click the **Authentication settings**, **Monitoring**, or **Test data management** tabs.
   Each page displays:
   - A yellow "Demo Mode" banner with a link to Admin Access
   - Realistic mock data pre-populated in all fields and charts
   - Fully rendered buttons (clicking them has no effect)

5. **Transition to live mode**

   Click the **Admin access** tab, enter your `AUTH_SETTINGS_ADMIN_KEY` value, and click **Request admin session**. After signing in, navigate back to any admin tab — the banner disappears and live data loads.

---

## Steps: Open Mode (No Admin Key — Demo Mode Suppressed)

1. Remove or unset `AUTH_SETTINGS_ADMIN_KEY` in `backend/.env`
2. Restart the backend
3. Open the frontend without signing in

All admin tabs display live data immediately — demo mode does not activate.

---

## Steps: Backend Offline Scenario (FR-014)

1. Start the frontend without the backend running
2. Open the app and navigate to any admin tab

The demo mode pages still render with mock data. The "Demo Mode" banner appears (the probe defaults to admin-key-required on network error). The **Admin access** tab shows an authentication error if you attempt to sign in.

---

## Running Tests

```bash
cd frontend
npm run test    # single run
npm run test:watch  # watch mode
```

Demo mode–specific tests are in:
- `src/DemoBanner.test.tsx`
- `src/AuthSettingsPage.test.tsx` (demo mode cases)
- `src/MonitoringPage.test.tsx` (demo mode cases)
- `src/TestDataPage.test.tsx` (demo mode cases)
