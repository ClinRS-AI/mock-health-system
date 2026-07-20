import React, { useEffect, useState } from "react";
import {
  getRandomTestPatient,
  lookupTestPatient,
  updateTestPatient,
  type LookupPatientResponse,
  lookupTestStudy,
  getRandomTestStudy,
  type StudyViewModel
} from "./api";
import { useAdminSession } from "./AdminSessionContext";

interface TestDataManipulationSectionProps {
  /** Called whenever an unsaved patient-record edit starts/ends, so the tab
   * orchestrator can warn before discarding it on a tab switch. */
  onEditingChange?: React.Dispatch<React.SetStateAction<boolean>>;
}

const TestDataManipulationSection: React.FC<TestDataManipulationSectionProps> = ({ onEditingChange }) => {
  const { isDemoMode } = useAdminSession();

  const [error, setError] = useState<string | null>(null);

  const [lookupForm, setLookupForm] = useState({ id: "", uid: "", email: "" });
  const [lookupResult, setLookupResult] = useState<LookupPatientResponse | null>(null);
  const [lookupEditJson, setLookupEditJson] = useState("");
  const [isEditingLookup, setIsEditingLookup] = useState(false);
  const [lookupNotFound, setLookupNotFound] = useState(false);
  const [loadingLookup, setLoadingLookup] = useState(false);
  const [savingLookupMode, setSavingLookupMode] = useState<"none" | "save" | "audit">("none");

  useEffect(() => {
    onEditingChange?.(isEditingLookup);
    return () => onEditingChange?.(false);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isEditingLookup]);

  const [studiesLookupForm, setStudiesLookupForm] = useState({ name: "", identifier: "", protocolNumber: "" });
  const [studiesLookupResult, setStudiesLookupResult] = useState<StudyViewModel | null>(null);
  const [studiesLookupNotFound, setStudiesLookupNotFound] = useState(false);
  const [loadingStudiesLookup, setLoadingStudiesLookup] = useState(false);

  async function handleLookupPatient(e: React.FormEvent) {
    e.preventDefault();
    if (isDemoMode) return;
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

      const result = await lookupTestPatient(params);
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
    if (isDemoMode) return;
    try {
      setLoadingLookup(true);
      setError(null);
      setLookupNotFound(false);

      const result = await getRandomTestPatient();
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
    if (isDemoMode) return;
    if (!lookupResult) return;

    try {
      setSavingLookupMode(withAudit ? "audit" : "save");
      setError(null);

      const parsed = JSON.parse(lookupEditJson) as LookupPatientResponse;
      const updated = await updateTestPatient(lookupResult.id, parsed, withAudit);
      const refreshed = await lookupTestPatient({ id: updated.id });

      setLookupResult(refreshed);
      setLookupEditJson(JSON.stringify(refreshed, null, 2));
      setIsEditingLookup(false);
    } catch (err) {
      console.error(err);
      setError("Unable to save patient record. Ensure JSON is valid and check admin access.");
    } finally {
      setSavingLookupMode("none");
    }
  }

  async function handleLookupStudy(e: React.FormEvent) {
    e.preventDefault();
    if (isDemoMode) return;
    const nameTrim = studiesLookupForm.name.trim();
    const identifierTrim = studiesLookupForm.identifier.trim();
    const protocolTrim = studiesLookupForm.protocolNumber.trim();
    const params = nameTrim
      ? { name: nameTrim }
      : identifierTrim
        ? { identifier: identifierTrim }
        : protocolTrim
          ? { protocolNumber: protocolTrim }
          : undefined;
    if (!params) return;
    try {
      setLoadingStudiesLookup(true);
      setError(null);
      setStudiesLookupResult(null);
      setStudiesLookupNotFound(false);

      const result = await lookupTestStudy(params);
      setStudiesLookupResult(result);
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number } })?.response?.status;
      if (status === 404) {
        setStudiesLookupNotFound(true);
        setStudiesLookupResult(null);
      } else {
        console.error(err);
        setError("Unable to lookup study. Check the admin key and backend.");
        setStudiesLookupResult(null);
      }
    } finally {
      setLoadingStudiesLookup(false);
    }
  }

  async function handleGetRandomStudy() {
    if (isDemoMode) return;
    try {
      setLoadingStudiesLookup(true);
      setError(null);
      setStudiesLookupNotFound(false);

      const result = await getRandomTestStudy();
      setStudiesLookupResult(result);
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number } })?.response?.status;
      if (status === 404) {
        setStudiesLookupNotFound(true);
        setStudiesLookupResult(null);
      } else {
        console.error(err);
        setError("Unable to get a random study. Check the admin key and backend.");
        setStudiesLookupResult(null);
      }
    } finally {
      setLoadingStudiesLookup(false);
    }
  }

  return (
    <div className="space-y-4">
      <section>
        <h2 className="text-sm font-medium text-slate-700">Data Manipulation</h2>
        <p className="text-xs text-slate-500">
          Look up a specific patient or study, view its full details, and edit patient records.
        </p>
      </section>

      {error && <div className="text-sm text-red-600">{error}</div>}

      <section className="rounded-lg border border-slate-200 bg-white p-4 space-y-3">
        <h3 className="text-sm font-semibold text-slate-800">Lookup patient</h3>
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
      </section>

      <section className="rounded-lg border border-slate-200 bg-white p-4 space-y-3">
        <h3 className="text-sm font-semibold text-slate-800">Lookup study</h3>
        <p className="text-xs text-slate-500">
          Find a study by name, identifier, or protocol number fragment (provide one). Uses the
          first value provided.
        </p>
        <form onSubmit={handleLookupStudy} className="space-y-3 max-w-sm">
          <label className="block text-xs text-slate-700">
            <span className="block mb-1">Name</span>
            <input
              type="text"
              placeholder="e.g. Acme Oncology"
              className="w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
              value={studiesLookupForm.name}
              onChange={(e) => setStudiesLookupForm((f) => ({ ...f, name: e.target.value }))}
            />
          </label>
          <label className="block text-xs text-slate-700">
            <span className="block mb-1">Identifier</span>
            <input
              type="text"
              className="w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
              value={studiesLookupForm.identifier}
              onChange={(e) => setStudiesLookupForm((f) => ({ ...f, identifier: e.target.value }))}
            />
          </label>
          <label className="block text-xs text-slate-700">
            <span className="block mb-1">Protocol number</span>
            <input
              type="text"
              placeholder="e.g. PROTO-2026"
              className="w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-sky-500"
              value={studiesLookupForm.protocolNumber}
              onChange={(e) =>
                setStudiesLookupForm((f) => ({ ...f, protocolNumber: e.target.value }))
              }
            />
          </label>
          <button
            type="submit"
            disabled={loadingStudiesLookup}
            className="inline-flex items-center justify-center rounded-md bg-slate-700 px-3 py-1.5 text-xs font-medium text-white shadow-sm hover:bg-slate-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-500 disabled:opacity-70"
          >
            {loadingStudiesLookup ? "Looking up…" : "Lookup"}
          </button>
          <button
            type="button"
            onClick={() => void handleGetRandomStudy()}
            disabled={loadingStudiesLookup}
            className="ml-2 inline-flex items-center justify-center rounded-md border border-slate-300 px-3 py-1.5 text-xs font-medium text-slate-700 shadow-sm hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-500 disabled:opacity-70"
          >
            {loadingStudiesLookup ? "Loading…" : "Get random"}
          </button>
        </form>
        {studiesLookupNotFound && (
          <div className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-800">
            No study found for the given name, identifier, or protocol number.
          </div>
        )}
        {studiesLookupResult && (
          <div className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-800 space-y-2">
            <div className="font-semibold">Study record</div>
            <pre className="max-h-[420px] overflow-auto rounded bg-slate-900 p-2 text-[11px] text-slate-100 whitespace-pre-wrap">
              {JSON.stringify(studiesLookupResult, null, 2)}
            </pre>
          </div>
        )}
      </section>
    </div>
  );
};

export default TestDataManipulationSection;
