import React, { useEffect, useState } from "react";
import {
  generateTestPatients,
  generateTestStaff,
  generateRecentAuditEvents,
  resetTestPatients,
  addTestPatient,
  getRandomTestPatient,
  lookupTestPatient,
  updateTestPatient,
  type GeneratePatientsOptions,
  type GeneratePatientsResult,
  type GenerateStaffOptions,
  type GenerateStaffResult,
  type GenerateRecentAuditEventsOptions,
  type GenerateRecentAuditEventsResult,
  type AddTestPatientResponse,
  type LookupPatientResponse,
  getPatientTestDataStats,
  type PatientTestDataStats
} from "./api";

const TestDataPage: React.FC = () => {
  const [adminKey, setAdminKey] = useState("");

  const [generateOptions, setGenerateOptions] = useState<GeneratePatientsOptions>({
    totalCount: 5000,
    duplicatePercentage: 3
  });

  const [generateResult, setGenerateResult] = useState<GeneratePatientsResult | null>(null);
  const [staffGenerateOptions, setStaffGenerateOptions] = useState<GenerateStaffOptions>({
    count: 10
  });
  const [staffGenerateResult, setStaffGenerateResult] = useState<GenerateStaffResult | null>(null);
  const [auditEventsGenerateOptions, setAuditEventsGenerateOptions] =
    useState<GenerateRecentAuditEventsOptions>({
      count: 25
    });
  const [auditEventsGenerateResult, setAuditEventsGenerateResult] =
    useState<GenerateRecentAuditEventsResult | null>(null);

  const [stats, setStats] = useState<PatientTestDataStats | null>(null);

  const [loadingReset, setLoadingReset] = useState(false);
  const [loadingGenerate, setLoadingGenerate] = useState(false);
  const [loadingGenerateStaff, setLoadingGenerateStaff] = useState(false);
  const [loadingGenerateAuditEvents, setLoadingGenerateAuditEvents] = useState(false);
  const [loadingAdd, setLoadingAdd] = useState(false);
  const [addResult, setAddResult] = useState<AddTestPatientResponse | null>(null);
  const [addForm, setAddForm] = useState({ firstName: "", lastName: "", email: "" });
  const [lookupForm, setLookupForm] = useState({ id: "", uid: "", email: "" });
  const [lookupResult, setLookupResult] = useState<LookupPatientResponse | null>(null);
  const [lookupEditJson, setLookupEditJson] = useState("");
  const [isEditingLookup, setIsEditingLookup] = useState(false);
  const [lookupNotFound, setLookupNotFound] = useState(false);
  const [loadingLookup, setLoadingLookup] = useState(false);
  const [savingLookupMode, setSavingLookupMode] = useState<"none" | "save" | "audit">("none");
  const [error, setError] = useState<string | null>(null);

  async function loadStats(currentAdminKey: string) {
    try {
      const data = await getPatientTestDataStats(currentAdminKey || undefined);
      setStats(data);
    } catch (err) {
      console.error(err);
      // Don't overwrite an existing, more specific error; just keep stats null.
      setStats(null);
    }
  }

  useEffect(() => {
    void loadStats(adminKey);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function handleReset() {
    try {
      setLoadingReset(true);
      setError(null);
      setGenerateResult(null);

      await resetTestPatients(adminKey || undefined);
      await loadStats(adminKey);
    } catch (err) {
      console.error(err);
      setError("Unable to reset patients. Check the admin key and backend.");
    } finally {
      setLoadingReset(false);
    }
  }

  async function handleGenerate() {
    try {
      setLoadingGenerate(true);
      setError(null);

      const result = await generateTestPatients(generateOptions, adminKey || undefined);
      setGenerateResult(result);
      await loadStats(adminKey);
    } catch (err) {
      console.error(err);
      setError("Unable to generate patients. Check the admin key and backend.");
      setGenerateResult(null);
    } finally {
      setLoadingGenerate(false);
    }
  }

  async function handleGenerateStaff() {
    try {
      setLoadingGenerateStaff(true);
      setError(null);

      const result = await generateTestStaff(staffGenerateOptions, adminKey || undefined);
      setStaffGenerateResult(result);
      await loadStats(adminKey);
    } catch (err) {
      console.error(err);
      setError("Unable to generate staff. Check the admin key and backend.");
      setStaffGenerateResult(null);
    } finally {
      setLoadingGenerateStaff(false);
    }
  }

  async function handleGenerateRecentAuditEvents() {
    try {
      setLoadingGenerateAuditEvents(true);
      setError(null);

      const result = await generateRecentAuditEvents(auditEventsGenerateOptions, adminKey || undefined);
      setAuditEventsGenerateResult(result);
      await loadStats(adminKey);
    } catch (err) {
      console.error(err);
      setError("Unable to generate recent audit events. Ensure staff (and patients if needed) exist.");
      setAuditEventsGenerateResult(null);
    } finally {
      setLoadingGenerateAuditEvents(false);
    }
  }

  async function handleAddPatient(e: React.FormEvent) {
    e.preventDefault();
    if (!addForm.firstName.trim() || !addForm.lastName.trim() || !addForm.email.trim()) return;
    try {
      setLoadingAdd(true);
      setError(null);
      setAddResult(null);

      const result = await addTestPatient(
        {
          firstName: addForm.firstName.trim(),
          lastName: addForm.lastName.trim(),
          email: addForm.email.trim()
        },
        adminKey || undefined
      );
      setAddResult(result);
      setAddForm({ firstName: "", lastName: "", email: "" });
      await loadStats(adminKey);
    } catch (err) {
      console.error(err);
      setError("Unable to add patient. Check the admin key and backend.");
      setAddResult(null);
    } finally {
      setLoadingAdd(false);
    }
  }

  async function handleLookupPatient(e: React.FormEvent) {
    e.preventDefault();
    const idTrim = lookupForm.id.trim();
    const uidTrim = lookupForm.uid.trim();
    const emailTrim = lookupForm.email.trim();
    const idVal = idTrim ? Number(idTrim) : undefined;
    if (idVal !== undefined && Number.isNaN(idVal)) return;
    const params =
      idVal !== undefined
        ? { id: idVal }
        : uidTrim
          ? { uid: uidTrim }
          : emailTrim
            ? { email: emailTrim }
            : undefined;
    if (!params) return;
    try {
      setLoadingLookup(true);
      setError(null);
      setLookupResult(null);
      setLookupNotFound(false);

      const result = await lookupTestPatient(params, adminKey || undefined);
      setLookupResult(result);
      setLookupEditJson(JSON.stringify(result, null, 2));
      setIsEditingLookup(false);
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number } })?.response?.status;
      if (status === 404) {
        setLookupNotFound(true);
        setLookupResult(null);
      } else {
        console.error(err);
        setError("Unable to lookup patient. Check the admin key and backend.");
        setLookupResult(null);
      }
    } finally {
      setLoadingLookup(false);
    }
  }

  async function handleGetRandomPatient() {
    try {
      setLoadingLookup(true);
      setError(null);
      setLookupNotFound(false);

      const result = await getRandomTestPatient(adminKey || undefined);
      setLookupResult(result);
      setLookupEditJson(JSON.stringify(result, null, 2));
      setIsEditingLookup(false);
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number } })?.response?.status;
      if (status === 404) {
        setLookupNotFound(true);
        setLookupResult(null);
      } else {
        console.error(err);
        setError("Unable to get a random patient. Check the admin key and backend.");
      }
    } finally {
      setLoadingLookup(false);
    }
  }

  async function handleSavePatientRecord(withAudit: boolean) {
    if (!lookupResult) return;

    try {
      setSavingLookupMode(withAudit ? "audit" : "save");
      setError(null);

      const parsed = JSON.parse(lookupEditJson) as LookupPatientResponse;
      const updated = await updateTestPatient(
        lookupResult.id,
        parsed,
        adminKey || undefined,
        withAudit
      );
      const refreshed = await lookupTestPatient({ id: updated.id }, adminKey || undefined);

      setLookupResult(refreshed);
      setLookupEditJson(JSON.stringify(refreshed, null, 2));
      setIsEditingLookup(false);
      await loadStats(adminKey);
    } catch (err) {
      console.error(err);
      setError("Unable to save patient record. Ensure JSON is valid and check admin access.");
    } finally {
      setSavingLookupMode("none");
    }
  }

  return (
    <div className="space-y-4">
      <section className="space-y-3">
        <div>
          <h2 className="text-sm font-medium text-slate-700">Test data management</h2>
          <p className="text-xs text-slate-500">
            Generate large volumes of synthetic patients (with controlled duplicates). Reset clears
            patient data; use Generate patients again for a consistent dataset.
          </p>
        </div>

        <div className="flex flex-col sm:flex-row sm:items-center gap-3">
          <input
            type="password"
            className="flex-1 rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
            placeholder="Admin key (X-Admin-Key) if configured"
            value={adminKey}
            onChange={(e) => {
              const value = e.target.value;
              setAdminKey(value);
              void loadStats(value);
            }}
          />
          <button
            type="button"
            onClick={() => void loadStats(adminKey)}
            className="inline-flex items-center justify-center rounded-md border border-slate-300 px-3 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50"
          >
            Refresh
          </button>
        </div>
      </section>

      {stats && (
        <section className="grid gap-4 md:grid-cols-4">
          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="text-xs font-medium text-slate-500">Patient count</p>
            <p className="mt-1 text-xl font-semibold text-slate-800 tabular-nums">
              {stats.patientCount}
            </p>
          </div>
          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="text-xs font-medium text-slate-500">Duplicate patients</p>
            <p className="mt-1 text-xl font-semibold text-slate-800 tabular-nums">
              {stats.duplicatePatientCount}
            </p>
          </div>
          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="text-xs font-medium text-slate-500">Recent audit events (last 5 min)</p>
            <p className="mt-1 text-xl font-semibold text-slate-800 tabular-nums">
              {stats.recentAuditEventCount}
            </p>
          </div>
          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="text-xs font-medium text-slate-500 mb-1">Patients by site</p>
            {stats.patientsBySite.length > 0 ? (
              <ul className="max-h-28 overflow-auto space-y-1 text-xs text-slate-700">
                {stats.patientsBySite.map((s) => (
                  <li key={s.siteName} className="flex justify-between gap-2">
                    <span className="truncate">{s.siteName}</span>
                    <span className="font-semibold tabular-nums">{s.count}</span>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="text-xs text-slate-400">No patients yet.</p>
            )}
          </div>
          <div className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="text-xs font-medium text-slate-500">Total staff</p>
            <p className="mt-1 text-xl font-semibold text-slate-800 tabular-nums">
              {stats.totalStaffCount}
            </p>
          </div>
        </section>
      )}

      {error && <div className="text-sm text-red-600">{error}</div>}

      <section className="grid gap-4 md:grid-cols-2">
        <div className="rounded-lg border border-slate-200 bg-white p-4 space-y-3">
          <h3 className="text-sm font-semibold text-slate-800">Reset patients</h3>
          <p className="text-xs text-slate-500">
            Truncate all patient-related tables. Use Generate patients to repopulate with a
            consistent dataset.
          </p>
          <button
            type="button"
            onClick={() => void handleReset()}
            disabled={loadingReset}
            className="inline-flex items-center justify-center rounded-md bg-rose-600 px-3 py-1.5 text-xs font-medium text-white shadow-sm hover:bg-rose-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rose-500"
          >
            {loadingReset ? "Resetting…" : "Reset patient data"}
          </button>
        </div>

        <div className="rounded-lg border border-slate-200 bg-white p-4 space-y-3">
          <h3 className="text-sm font-semibold text-slate-800">Generate patients</h3>
          <p className="text-xs text-slate-500">
            Generate synthetic patients plus a percentage of near-duplicates (different names,
            addresses, and contact details).
          </p>

          <div className="space-y-2 text-xs text-slate-700">
            <label className="block">
              <span className="block mb-1">Total patients to generate</span>
              <input
                type="number"
                min={1}
                className="w-full rounded-md border border-slate-300 px-2 py-1 text-xs focus:outline-none focus:ring-2 focus:ring-sky-500"
                value={generateOptions.totalCount ?? ""}
                onChange={(e) =>
                  setGenerateOptions((prev) => ({
                    ...prev,
                    totalCount: e.target.value === "" ? undefined : Number(e.target.value)
                  }))
                }
              />
            </label>

            <label className="block">
              <span className="block mb-1">Duplicate percentage</span>
              <input
                type="number"
                min={0}
                max={100}
                className="w-full rounded-md border border-slate-300 px-2 py-1 text-xs focus:outline-none focus:ring-2 focus:ring-sky-500"
                value={generateOptions.duplicatePercentage ?? ""}
                onChange={(e) =>
                  setGenerateOptions((prev) => ({
                    ...prev,
                    duplicatePercentage: e.target.value === "" ? undefined : Number(e.target.value)
                  }))
                }
              />
            </label>

            <label className="block">
              <span className="block mb-1">Seed (optional, for reproducible data)</span>
              <input
                type="number"
                className="w-full rounded-md border border-slate-300 px-2 py-1 text-xs focus:outline-none focus:ring-2 focus:ring-sky-500"
                value={generateOptions.seed ?? ""}
                onChange={(e) =>
                  setGenerateOptions((prev) => ({
                    ...prev,
                    seed: e.target.value === "" ? undefined : Number(e.target.value)
                  }))
                }
              />
            </label>
          </div>

          <button
            type="button"
            onClick={() => void handleGenerate()}
            disabled={loadingGenerate}
            className="inline-flex items-center justify-center rounded-md bg-sky-600 px-3 py-1.5 text-xs font-medium text-white shadow-sm hover:bg-sky-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-500"
          >
            {loadingGenerate ? "Generating…" : "Generate patients"}
          </button>

          {generateResult && (
            <div className="mt-2 rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs text-emerald-800 space-y-1">
              <div>
                Requested:{" "}
                <span className="font-semibold tabular-nums">
                  {generateResult.totalRequested}
                </span>{" "}
                patients
              </div>
              <div>
                Base inserted:{" "}
                <span className="font-semibold tabular-nums">
                  {generateResult.totalBaseInserted}
                </span>
              </div>
              <div>
                Duplicates inserted:{" "}
                <span className="font-semibold tabular-nums">
                  {generateResult.duplicateInserted} (requested {generateResult.duplicateRequested})
                </span>
              </div>
              <div>
                Total after generation:{" "}
                <span className="font-semibold tabular-nums">{generateResult.totalAfter}</span>
              </div>
            </div>
          )}
        </div>
      </section>

      <section className="grid gap-4 md:grid-cols-2">
        <div className="rounded-lg border border-slate-200 bg-white p-4 space-y-3">
          <h3 className="text-sm font-semibold text-slate-800">Generate staff</h3>
          <p className="text-xs text-slate-500">
            Create synthetic staff records used by audit events and reporting.
          </p>
          <div className="space-y-2 text-xs text-slate-700">
            <label className="block">
              <span className="block mb-1">Staff count</span>
              <input
                type="number"
                min={1}
                className="w-full rounded-md border border-slate-300 px-2 py-1 text-xs focus:outline-none focus:ring-2 focus:ring-sky-500"
                value={staffGenerateOptions.count ?? ""}
                onChange={(e) =>
                  setStaffGenerateOptions((prev) => ({
                    ...prev,
                    count: e.target.value === "" ? undefined : Number(e.target.value)
                  }))
                }
              />
            </label>
            <label className="block">
              <span className="block mb-1">Seed (optional)</span>
              <input
                type="number"
                className="w-full rounded-md border border-slate-300 px-2 py-1 text-xs focus:outline-none focus:ring-2 focus:ring-sky-500"
                value={staffGenerateOptions.seed ?? ""}
                onChange={(e) =>
                  setStaffGenerateOptions((prev) => ({
                    ...prev,
                    seed: e.target.value === "" ? undefined : Number(e.target.value)
                  }))
                }
              />
            </label>
          </div>
          <button
            type="button"
            onClick={() => void handleGenerateStaff()}
            disabled={loadingGenerateStaff}
            className="inline-flex items-center justify-center rounded-md bg-sky-600 px-3 py-1.5 text-xs font-medium text-white shadow-sm hover:bg-sky-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-500"
          >
            {loadingGenerateStaff ? "Generating…" : "Generate staff"}
          </button>
          {staffGenerateResult && (
            <div className="mt-2 rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs text-emerald-800 space-y-1">
              <div>
                Staff inserted:{" "}
                <span className="font-semibold tabular-nums">{staffGenerateResult.inserted}</span>
              </div>
              <div>
                Total staff:{" "}
                <span className="font-semibold tabular-nums">{staffGenerateResult.totalAfter}</span>
              </div>
            </div>
          )}
        </div>
        <div className="rounded-lg border border-slate-200 bg-white p-4 space-y-3">
          <h3 className="text-sm font-semibold text-slate-800">Generate recent audit events</h3>
          <p className="text-xs text-slate-500">
            Create audit events with timestamps randomized within the last 5 minutes.
          </p>
          <div className="space-y-2 text-xs text-slate-700">
            <label className="block">
              <span className="block mb-1">Audit event count</span>
              <input
                type="number"
                min={1}
                className="w-full rounded-md border border-slate-300 px-2 py-1 text-xs focus:outline-none focus:ring-2 focus:ring-sky-500"
                value={auditEventsGenerateOptions.count ?? ""}
                onChange={(e) =>
                  setAuditEventsGenerateOptions((prev) => ({
                    ...prev,
                    count: e.target.value === "" ? undefined : Number(e.target.value)
                  }))
                }
              />
            </label>
            <label className="block">
              <span className="block mb-1">Seed (optional)</span>
              <input
                type="number"
                className="w-full rounded-md border border-slate-300 px-2 py-1 text-xs focus:outline-none focus:ring-2 focus:ring-sky-500"
                value={auditEventsGenerateOptions.seed ?? ""}
                onChange={(e) =>
                  setAuditEventsGenerateOptions((prev) => ({
                    ...prev,
                    seed: e.target.value === "" ? undefined : Number(e.target.value)
                  }))
                }
              />
            </label>
          </div>
          <button
            type="button"
            onClick={() => void handleGenerateRecentAuditEvents()}
            disabled={loadingGenerateAuditEvents}
            className="inline-flex items-center justify-center rounded-md bg-sky-600 px-3 py-1.5 text-xs font-medium text-white shadow-sm hover:bg-sky-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-500"
          >
            {loadingGenerateAuditEvents ? "Generating…" : "Generate recent audit events"}
          </button>
          {auditEventsGenerateResult && (
            <div className="mt-2 rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs text-emerald-800 space-y-1">
              <div>
                Audit events inserted:{" "}
                <span className="font-semibold tabular-nums">{auditEventsGenerateResult.inserted}</span>
              </div>
              <div>
                Total audit events:{" "}
                <span className="font-semibold tabular-nums">
                  {auditEventsGenerateResult.totalAfter}
                </span>
              </div>
            </div>
          )}
        </div>
      </section>

      <section className="rounded-lg border border-slate-200 bg-white">
        <details className="group">
          <summary className="cursor-pointer list-none px-4 py-3 text-sm font-semibold text-slate-800 hover:bg-slate-50">
            <span className="inline-flex items-center gap-2">
              <span className="text-slate-400 group-open:rotate-90 transition-transform" aria-hidden>
                ▶
              </span>
              Add test patient (manual)
            </span>
          </summary>
          <div className="border-t border-slate-200 px-4 py-3 space-y-3">
            <p className="text-xs text-slate-500">
              Add a single patient with minimal fields for testing email and authentication. Id and
              UID are generated by the system.
            </p>
            <form onSubmit={handleAddPatient} className="space-y-3 max-w-sm">
              <label className="block text-xs text-slate-700">
                <span className="block mb-1">First name</span>
                <input
                  type="text"
                  required
                  className="w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                  value={addForm.firstName}
                  onChange={(e) => setAddForm((f) => ({ ...f, firstName: e.target.value }))}
                />
              </label>
              <label className="block text-xs text-slate-700">
                <span className="block mb-1">Last name</span>
                <input
                  type="text"
                  required
                  className="w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                  value={addForm.lastName}
                  onChange={(e) => setAddForm((f) => ({ ...f, lastName: e.target.value }))}
                />
              </label>
              <label className="block text-xs text-slate-700">
                <span className="block mb-1">Email</span>
                <input
                  type="email"
                  required
                  className="w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                  value={addForm.email}
                  onChange={(e) => setAddForm((f) => ({ ...f, email: e.target.value }))}
                />
              </label>
              <button
                type="submit"
                disabled={loadingAdd}
                className="inline-flex items-center justify-center rounded-md bg-slate-700 px-3 py-1.5 text-xs font-medium text-white shadow-sm hover:bg-slate-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-500 disabled:opacity-70"
              >
                {loadingAdd ? "Adding…" : "Add patient"}
              </button>
            </form>
            {addResult && (
              <div className="rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs text-emerald-800">
                Patient added. Id:{" "}
                <span className="font-semibold tabular-nums">{addResult.id}</span>, UID:{" "}
                <span className="font-mono text-[11px]">{addResult.uid}</span>
              </div>
            )}
          </div>
        </details>
      </section>

      <section className="rounded-lg border border-slate-200 bg-white">
        <details className="group">
          <summary className="cursor-pointer list-none px-4 py-3 text-sm font-semibold text-slate-800 hover:bg-slate-50">
            <span className="inline-flex items-center gap-2">
              <span className="text-slate-400 group-open:rotate-90 transition-transform" aria-hidden>
                ▶
              </span>
              Lookup patient
            </span>
          </summary>
          <div className="border-t border-slate-200 px-4 py-3 space-y-3">
            <p className="text-xs text-slate-500">
              Find a patient by ID, UID, or email (provide one). Uses the first value provided.
            </p>
            <form onSubmit={handleLookupPatient} className="space-y-3 max-w-sm">
              <label className="block text-xs text-slate-700">
                <span className="block mb-1">ID</span>
                <input
                  type="text"
                  inputMode="numeric"
                  placeholder="e.g. 1"
                  className="w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                  value={lookupForm.id}
                  onChange={(e) => setLookupForm((f) => ({ ...f, id: e.target.value }))}
                />
              </label>
              <label className="block text-xs text-slate-700">
                <span className="block mb-1">UID</span>
                <input
                  type="text"
                  placeholder="e.g. 550e8400-e29b-41d4-a716-446655440000"
                  className="w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-sky-500"
                  value={lookupForm.uid}
                  onChange={(e) => setLookupForm((f) => ({ ...f, uid: e.target.value }))}
                />
              </label>
              <label className="block text-xs text-slate-700">
                <span className="block mb-1">Email</span>
                <input
                  type="text"
                  placeholder="e.g. user@example.com"
                  className="w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
                  value={lookupForm.email}
                  onChange={(e) => setLookupForm((f) => ({ ...f, email: e.target.value }))}
                />
              </label>
              <button
                type="submit"
                disabled={loadingLookup}
                className="inline-flex items-center justify-center rounded-md bg-slate-700 px-3 py-1.5 text-xs font-medium text-white shadow-sm hover:bg-slate-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-500 disabled:opacity-70"
              >
                {loadingLookup ? "Looking up…" : "Lookup"}
              </button>
              <button
                type="button"
                onClick={() => void handleGetRandomPatient()}
                disabled={loadingLookup}
                className="ml-2 inline-flex items-center justify-center rounded-md border border-slate-300 px-3 py-1.5 text-xs font-medium text-slate-700 shadow-sm hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-500 disabled:opacity-70"
              >
                {loadingLookup ? "Loading…" : "Get random"}
              </button>
            </form>
            {lookupNotFound && (
              <div className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-800">
                No patient found for the given ID, UID, or email.
              </div>
            )}
            {lookupResult && (
              <div className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-800 space-y-2">
                <div className="flex items-center justify-between">
                  <div className="font-semibold">Patient record (all details)</div>
                  <div className="flex items-center gap-2">
                    {!isEditingLookup ? (
                      <button
                        type="button"
                        className="inline-flex items-center justify-center rounded-md border border-slate-300 px-2 py-1 text-xs font-medium text-slate-700 hover:bg-slate-100"
                        onClick={() => setIsEditingLookup(true)}
                      >
                        Edit
                      </button>
                    ) : (
                      <>
                        <button
                          type="button"
                          className="inline-flex items-center justify-center rounded-md border border-slate-300 px-2 py-1 text-xs font-medium text-slate-700 hover:bg-slate-100"
                          onClick={() => {
                            setLookupEditJson(JSON.stringify(lookupResult, null, 2));
                            setIsEditingLookup(false);
                          }}
                        >
                          Cancel
                        </button>
                        <button
                          type="button"
                          disabled={savingLookupMode !== "none"}
                          className="inline-flex items-center justify-center rounded-md bg-sky-600 px-2 py-1 text-xs font-medium text-white hover:bg-sky-700 disabled:opacity-70"
                          onClick={() => void handleSavePatientRecord(false)}
                        >
                          {savingLookupMode === "save" ? "Saving…" : "Save"}
                        </button>
                        <button
                          type="button"
                          disabled={savingLookupMode !== "none"}
                          className="inline-flex items-center justify-center rounded-md bg-emerald-600 px-2 py-1 text-xs font-medium text-white hover:bg-emerald-700 disabled:opacity-70"
                          onClick={() => void handleSavePatientRecord(true)}
                        >
                          {savingLookupMode === "audit" ? "Saving…" : "Save with Audit"}
                        </button>
                      </>
                    )}
                  </div>
                </div>
                {isEditingLookup ? (
                  <textarea
                    className="min-h-[340px] w-full rounded-md border border-slate-300 bg-white p-2 font-mono text-[11px] focus:outline-none focus:ring-2 focus:ring-sky-500"
                    value={lookupEditJson}
                    onChange={(e) => setLookupEditJson(e.target.value)}
                  />
                ) : (
                  <pre className="max-h-[420px] overflow-auto rounded bg-slate-900 p-2 text-[11px] text-slate-100 whitespace-pre-wrap">
                    {JSON.stringify(lookupResult, null, 2)}
                  </pre>
                )}
              </div>
            )}
          </div>
        </details>
      </section>
    </div>
  );
};

export default TestDataPage;

