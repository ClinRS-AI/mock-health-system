import React, { useState } from "react";
import {
  generateTestPatients,
  generateTestStaff,
  generateRecentAuditEvents,
  addTestPatient,
  generateTestStudies,
  type GeneratePatientsOptions,
  type GeneratePatientsResult,
  type GenerateStaffOptions,
  type GenerateStaffResult,
  type GenerateRecentAuditEventsOptions,
  type GenerateRecentAuditEventsResult,
  type AddTestPatientResponse,
  type GenerateStudiesOptions,
  type GenerateStudiesResult
} from "./api";
import { useAdminSession } from "./AdminSessionContext";

const TestDataGenerationSection: React.FC = () => {
  const { isDemoMode } = useAdminSession();

  const [error, setError] = useState<string | null>(null);

  const [generateOptions, setGenerateOptions] = useState<GeneratePatientsOptions>({
    totalCount: 5000,
    duplicatePercentage: 3
  });
  const [generateResult, setGenerateResult] = useState<GeneratePatientsResult | null>(null);
  const [loadingGenerate, setLoadingGenerate] = useState(false);

  const [studiesGenerateOptions, setStudiesGenerateOptions] = useState<GenerateStudiesOptions>({
    totalCount: 25
  });
  const [studiesGenerateResult, setStudiesGenerateResult] = useState<GenerateStudiesResult | null>(null);
  const [loadingGenerateStudies, setLoadingGenerateStudies] = useState(false);

  const [staffGenerateOptions, setStaffGenerateOptions] = useState<GenerateStaffOptions>({
    count: 10
  });
  const [staffGenerateResult, setStaffGenerateResult] = useState<GenerateStaffResult | null>(null);
  const [loadingGenerateStaff, setLoadingGenerateStaff] = useState(false);

  const [auditEventsGenerateOptions, setAuditEventsGenerateOptions] =
    useState<GenerateRecentAuditEventsOptions>({
      count: 25
    });
  const [auditEventsGenerateResult, setAuditEventsGenerateResult] =
    useState<GenerateRecentAuditEventsResult | null>(null);
  const [loadingGenerateAuditEvents, setLoadingGenerateAuditEvents] = useState(false);

  const [addForm, setAddForm] = useState({ firstName: "", lastName: "", email: "" });
  const [addResult, setAddResult] = useState<AddTestPatientResponse | null>(null);
  const [loadingAdd, setLoadingAdd] = useState(false);

  async function handleGenerate() {
    if (isDemoMode) return;
    try {
      setLoadingGenerate(true);
      setError(null);

      const result = await generateTestPatients(generateOptions);
      setGenerateResult(result);
    } catch (err) {
      console.error(err);
      setError("Unable to generate patients. Check the admin key and backend.");
      setGenerateResult(null);
    } finally {
      setLoadingGenerate(false);
    }
  }

  async function handleGenerateStudies() {
    if (isDemoMode) return;
    try {
      setLoadingGenerateStudies(true);
      setError(null);

      const result = await generateTestStudies(studiesGenerateOptions);
      setStudiesGenerateResult(result);
    } catch (err) {
      console.error(err);
      setError("Unable to generate studies. Check the admin key and backend.");
      setStudiesGenerateResult(null);
    } finally {
      setLoadingGenerateStudies(false);
    }
  }

  async function handleGenerateStaff() {
    if (isDemoMode) return;
    try {
      setLoadingGenerateStaff(true);
      setError(null);

      const result = await generateTestStaff(staffGenerateOptions);
      setStaffGenerateResult(result);
    } catch (err) {
      console.error(err);
      setError("Unable to generate staff. Check the admin key and backend.");
      setStaffGenerateResult(null);
    } finally {
      setLoadingGenerateStaff(false);
    }
  }

  async function handleGenerateRecentAuditEvents() {
    if (isDemoMode) return;
    try {
      setLoadingGenerateAuditEvents(true);
      setError(null);

      const result = await generateRecentAuditEvents(auditEventsGenerateOptions);
      setAuditEventsGenerateResult(result);
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
    if (isDemoMode) return;
    if (!addForm.firstName.trim() || !addForm.lastName.trim() || !addForm.email.trim()) return;
    try {
      setLoadingAdd(true);
      setError(null);
      setAddResult(null);

      const result = await addTestPatient({
        firstName: addForm.firstName.trim(),
        lastName: addForm.lastName.trim(),
        email: addForm.email.trim()
      });
      setAddResult(result);
      setAddForm({ firstName: "", lastName: "", email: "" });
    } catch (err) {
      console.error(err);
      setError("Unable to add patient. Check the admin key and backend.");
      setAddResult(null);
    } finally {
      setLoadingAdd(false);
    }
  }

  return (
    <div className="space-y-4">
      <section>
        <h2 className="text-sm font-medium text-slate-700">Data Generation</h2>
        <p className="text-xs text-slate-500">
          Create synthetic patients, studies, staff, and audit events. Reset controls live in the
          Information and Destruction tab.
        </p>
      </section>

      {error && <div className="text-sm text-red-600">{error}</div>}

      <section className="grid gap-4 md:grid-cols-2">
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
            className="inline-flex items-center justify-center rounded-md bg-sky-600 px-3 py-1.5 text-xs font-medium text-white shadow-sm hover:bg-sky-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-500 disabled:opacity-70"
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

        <div className="rounded-lg border border-slate-200 bg-white p-4 space-y-3">
          <h3 className="text-sm font-semibold text-slate-800">Generate studies</h3>
          <p className="text-xs text-slate-500">
            Generate synthetic studies with populated structural sub-resources (arms, visits,
            milestones, documents, notes).
          </p>

          <div className="space-y-2 text-xs text-slate-700">
            <label className="block">
              <span className="block mb-1">Total studies to generate</span>
              <input
                type="number"
                min={1}
                className="w-full rounded-md border border-slate-300 px-2 py-1 text-xs focus:outline-none focus:ring-2 focus:ring-sky-500"
                value={studiesGenerateOptions.totalCount ?? ""}
                onChange={(e) =>
                  setStudiesGenerateOptions((prev) => ({
                    ...prev,
                    totalCount: e.target.value === "" ? undefined : Number(e.target.value)
                  }))
                }
              />
            </label>

            <label className="block">
              <span className="block mb-1">Seed (optional, for reproducible data)</span>
              <input
                type="number"
                className="w-full rounded-md border border-slate-300 px-2 py-1 text-xs focus:outline-none focus:ring-2 focus:ring-sky-500"
                value={studiesGenerateOptions.seed ?? ""}
                onChange={(e) =>
                  setStudiesGenerateOptions((prev) => ({
                    ...prev,
                    seed: e.target.value === "" ? undefined : Number(e.target.value)
                  }))
                }
              />
            </label>
          </div>

          <button
            type="button"
            onClick={() => void handleGenerateStudies()}
            disabled={loadingGenerateStudies}
            className="inline-flex items-center justify-center rounded-md bg-sky-600 px-3 py-1.5 text-xs font-medium text-white shadow-sm hover:bg-sky-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-500 disabled:opacity-70"
          >
            {loadingGenerateStudies ? "Generating…" : "Generate studies"}
          </button>

          {studiesGenerateResult && (
            <div className="mt-2 rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs text-emerald-800 space-y-1">
              <div>
                Requested:{" "}
                <span className="font-semibold tabular-nums">
                  {studiesGenerateResult.totalRequested}
                </span>{" "}
                studies
              </div>
              <div>
                Inserted:{" "}
                <span className="font-semibold tabular-nums">
                  {studiesGenerateResult.totalInserted}
                </span>{" "}
                (arms {studiesGenerateResult.armsInserted}, visits{" "}
                {studiesGenerateResult.visitsInserted}, milestones{" "}
                {studiesGenerateResult.milestonesInserted}, documents{" "}
                {studiesGenerateResult.documentsInserted}, notes {studiesGenerateResult.notesInserted})
              </div>
              <div>
                Total after generation:{" "}
                <span className="font-semibold tabular-nums">
                  {studiesGenerateResult.totalAfter}
                </span>
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
            className="inline-flex items-center justify-center rounded-md bg-sky-600 px-3 py-1.5 text-xs font-medium text-white shadow-sm hover:bg-sky-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-500 disabled:opacity-70"
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
            className="inline-flex items-center justify-center rounded-md bg-sky-600 px-3 py-1.5 text-xs font-medium text-white shadow-sm hover:bg-sky-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-500 disabled:opacity-70"
          >
            {loadingGenerateAuditEvents ? "Generating…" : "Generate recent audit events"}
          </button>
          {auditEventsGenerateResult && (
            <div className="mt-2 rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs text-emerald-800 space-y-2">
              <div>
                Audit events inserted:{" "}
                <span className="font-semibold tabular-nums">{auditEventsGenerateResult.inserted}</span>
              </div>
              {(auditEventsGenerateResult.insertedByAuditType ?? []).length > 0 && (
                <div>
                  <div className="font-medium text-emerald-900 mb-1">By audit type</div>
                  <ul className="space-y-0.5 max-h-40 overflow-auto border border-emerald-100 rounded bg-white/60 px-2 py-1">
                    {(auditEventsGenerateResult.insertedByAuditType ?? []).map((row) => (
                      <li key={row.code} className="flex justify-between gap-3">
                        <span className="truncate font-mono text-[11px]" title={row.displayName}>
                          {row.code}
                        </span>
                        <span className="font-semibold tabular-nums shrink-0">{row.count}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
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

      <section className="rounded-lg border border-slate-200 bg-white p-4 space-y-3">
        <h3 className="text-sm font-semibold text-slate-800">Add test patient (manual)</h3>
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
      </section>
    </div>
  );
};

export default TestDataGenerationSection;
