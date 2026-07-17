# Quickstart: Test Data Management Dashboard Reorganization

Assumes the mock system is already running per the top-level project quickstart
(`specs/feature/001-cc-api-mock/quickstart.md`) — backend on `http://localhost:5001`,
frontend on `http://localhost:5176`. This guide covers manually verifying the
reorganized Test Data page.

---

## 1. Start the frontend

```bash
cd frontend
npm run dev
```

Open `http://localhost:5176`, sign in via **Admin access** if an admin key is
configured (or continue in demo mode if unauthenticated).

---

## 2. Open the Test Data page and confirm the four tabs

Click **Test data management** in the top nav. Confirm four tabs are visible:
**Data Counts and Visualizations**, **Data Generation**, **Data Manipulation**,
**Information and Destruction** — with exactly one area's content shown at a time.

---

## 3. Data Counts and Visualizations

- Confirm patient stat cards (patient count, duplicates, recent audit events, total
  staff) and study stat cards (study count, arms, visits) are visible.
- Confirm "patients by site" and "studies by status" render as small charts
  (Recharts), not plain text lists.
- Click **Refresh stats** and confirm the numbers reload without a full page
  refresh.

---

## 4. Data Generation

- Generate 10 patients (small count, e.g. `totalCount: 10`) and confirm a result
  summary appears in this tab.
- Generate 5 studies and confirm a result summary appears.
- Generate staff and recent audit events; confirm each has its own result summary.
- Add a manual test patient (first name, last name, email) and confirm the
  returned id/UID display.
- Confirm no reset/destructive button is visible anywhere in this tab.

---

## 5. Data Manipulation

- Look up a patient by ID (use an id from step 4's bulk generation, or click
  "Get random"). Confirm full patient JSON detail displays.
- Click **Edit**, change a field, **Save**. Confirm the change persists (re-lookup
  shows the new value).
- Look up a study by name fragment (or click "Get random"). Confirm full study
  JSON detail displays.
- Confirm no generation form or reset button is visible anywhere in this tab.

---

## 6. Information and Destruction

- Confirm the JSON API base, SOAP endpoint, and SOAP WSDL URLs display with
  working **Copy** buttons.
- Confirm SOAP report pkeys list and refresh.
- In the **danger zone**, click **Reset patient data**. Confirm a confirmation
  step appears (button changes to a confirm state, or a confirm/cancel pair
  appears) — the reset must NOT fire immediately on the first click.
- Confirm the reset, then verify (via Data Counts and Visualizations) that patient
  data is cleared and study data is untouched.
- Repeat for **Reset study data**, verifying study data clears and patient data is
  untouched.

---

## 7. Demo mode check

Sign out / load the page unauthenticated (demo mode). Confirm all four tabs
still render with demo/read-only content, and that every generate/reset/lookup/edit
control is disabled or inert (no live API calls fire — check the Network tab).

---

## 8. Mobile viewport check

Resize the browser (or use devtools device emulation) to a narrow width (e.g.
375px). Confirm all four tabs remain visible/reachable and each area's content
is usable without horizontal scrolling breaking functionality.

---

## 9. Run the automated test suite

```bash
cd frontend
npm run test
npm run lint
```

All existing Test Data page test scenarios should now pass from their new
colocated files (`TestDataCountsSection.test.tsx`,
`TestDataGenerationSection.test.tsx`, `TestDataManipulationSection.test.tsx`,
`TestDataInfoDestructionSection.test.tsx`) plus orchestration tests in
`TestDataPage.test.tsx`.
