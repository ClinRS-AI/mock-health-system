import React, { useEffect, useState } from "react";
import {
  getSoapReportPkeys,
  getConfiguredApiBaseUrl,
  resetTestPatients,
  resetTestStudies
} from "./api";
import { useAdminSession } from "./AdminSessionContext";

interface ConfirmableResetButtonProps {
  label: string;
  confirming: boolean;
  loading: boolean;
  isDemoMode: boolean;
  onStart: () => void;
  onConfirm: () => void;
  onCancel: () => void;
}

function ConfirmableResetButton({
  label,
  confirming,
  loading,
  isDemoMode,
  onStart,
  onConfirm,
  onCancel
}: ConfirmableResetButtonProps) {
  if (!confirming) {
    return (
      <button
        type="button"
        onClick={() => {
          if (!isDemoMode) onStart();
        }}
        disabled={loading}
        className="inline-flex items-center justify-center rounded-md bg-rose-600 px-3 py-1.5 text-xs font-medium text-white shadow-sm hover:bg-rose-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rose-500 disabled:opacity-70"
      >
        {label}
      </button>
    );
  }
  return (
    <div className="flex items-center gap-2">
      <button
        type="button"
        onClick={onConfirm}
        disabled={loading}
        className="inline-flex items-center justify-center rounded-md bg-rose-700 px-3 py-1.5 text-xs font-semibold text-white shadow-sm hover:bg-rose-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rose-500 disabled:opacity-70"
      >
        {loading ? "Resetting…" : "Confirm reset"}
      </button>
      <button
        type="button"
        onClick={onCancel}
        disabled={loading}
        className="inline-flex items-center justify-center rounded-md border border-slate-300 px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-70"
      >
        Cancel
      </button>
    </div>
  );
}

const TestDataInfoDestructionSection: React.FC = () => {
  const { hasSession, isDemoMode, isProbeSettled } = useAdminSession();

  const [soapPkeys, setSoapPkeys] = useState<string[] | null>(null);
  const [soapPkeysError, setSoapPkeysError] = useState<string | null>(null);
  const [loadingSoapPkeys, setLoadingSoapPkeys] = useState(false);

  const [error, setError] = useState<string | null>(null);
  const [loadingReset, setLoadingReset] = useState(false);
  const [resetConfirming, setResetConfirming] = useState(false);
  const [loadingResetStudies, setLoadingResetStudies] = useState(false);
  const [resetStudiesConfirming, setResetStudiesConfirming] = useState(false);

  const apiBaseUrl = getConfiguredApiBaseUrl();
  const versionedJsonBase = apiBaseUrl ? `${apiBaseUrl}/api/v1` : "";
  const soapPostUrl = apiBaseUrl ? `${apiBaseUrl}/soap/report` : "";
  const soapWsdlUrl = apiBaseUrl ? `${apiBaseUrl}/soap/report?wsdl` : "";

  async function loadSoapPkeys() {
    try {
      setLoadingSoapPkeys(true);
      setSoapPkeysError(null);
      const data = await getSoapReportPkeys();
      setSoapPkeys(data.pkeys);
    } catch (err) {
      console.error(err);
      setSoapPkeys(null);
      setSoapPkeysError(
        "Unable to load SOAP pkeys. Use Admin access to sign in if the server requires an admin key."
      );
    } finally {
      setLoadingSoapPkeys(false);
    }
  }

  useEffect(() => {
    if (!hasSession && !isProbeSettled) return;
    if (!isDemoMode) void loadSoapPkeys();
  }, [hasSession, isDemoMode, isProbeSettled]);

  async function copyToClipboard(text: string): Promise<void> {
    try {
      await navigator.clipboard.writeText(text);
    } catch {
      // Clipboard may be denied; ignore.
    }
  }

  async function handleReset() {
    if (isDemoMode) return;
    try {
      setLoadingReset(true);
      setError(null);
      await resetTestPatients();
    } catch (err) {
      console.error(err);
      setError("Unable to reset patients. Check the admin key and backend.");
    } finally {
      setLoadingReset(false);
      setResetConfirming(false);
    }
  }

  async function handleResetStudies() {
    if (isDemoMode) return;
    try {
      setLoadingResetStudies(true);
      setError(null);
      await resetTestStudies();
    } catch (err) {
      console.error(err);
      setError("Unable to reset studies. Check the admin key and backend.");
    } finally {
      setLoadingResetStudies(false);
      setResetStudiesConfirming(false);
    }
  }

  return (
    <div className="space-y-4">
      <section>
        <h2 className="text-sm font-medium text-slate-700">Information and Destruction</h2>
        <p className="text-xs text-slate-500">
          Connection details for this instance, and destructive data-reset controls.
        </p>
      </section>

      {error && <div className="text-sm text-red-600">{error}</div>}

      <section className="rounded-lg border border-slate-200 bg-slate-50 p-4 space-y-3">
        <h3 className="text-sm font-semibold text-slate-800">API &amp; SOAP (this instance)</h3>
        <p className="text-xs text-slate-500">
          URLs are derived from <code className="text-[11px]">VITE_API_BASE_URL</code> (same origin this UI uses for
          JSON calls). SOAP lives outside <code className="text-[11px]">/api/v1</code>.
        </p>
        <dl className="space-y-3 text-xs">
          <div>
            <dt className="font-medium text-slate-600 mb-1">JSON API base</dt>
            <dd className="flex flex-col sm:flex-row sm:items-start gap-2">
              <code className="block flex-1 break-all rounded border border-slate-200 bg-white px-2 py-1.5 text-slate-800">
                {versionedJsonBase || "Set VITE_API_BASE_URL in frontend .env"}
              </code>
              {versionedJsonBase ? (
                <button
                  type="button"
                  onClick={() => void copyToClipboard(versionedJsonBase)}
                  className="shrink-0 rounded border border-slate-300 px-2 py-1 font-medium text-slate-700 hover:bg-white"
                >
                  Copy
                </button>
              ) : null}
            </dd>
          </div>
          <div>
            <dt className="font-medium text-slate-600 mb-1">SOAP endpoint (POST RunReport)</dt>
            <dd className="flex flex-col sm:flex-row sm:items-start gap-2">
              <code className="block flex-1 break-all rounded border border-slate-200 bg-white px-2 py-1.5 text-slate-800">
                {soapPostUrl || "—"}
              </code>
              {soapPostUrl ? (
                <button
                  type="button"
                  onClick={() => void copyToClipboard(soapPostUrl)}
                  className="shrink-0 rounded border border-slate-300 px-2 py-1 font-medium text-slate-700 hover:bg-white"
                >
                  Copy
                </button>
              ) : null}
            </dd>
          </div>
          <div>
            <dt className="font-medium text-slate-600 mb-1">SOAP WSDL</dt>
            <dd className="flex flex-col sm:flex-row sm:items-start gap-2">
              <code className="block flex-1 break-all rounded border border-slate-200 bg-white px-2 py-1.5 text-slate-800">
                {soapWsdlUrl || "—"}
              </code>
              {soapWsdlUrl ? (
                <button
                  type="button"
                  onClick={() => void copyToClipboard(soapWsdlUrl)}
                  className="shrink-0 rounded border border-slate-300 px-2 py-1 font-medium text-slate-700 hover:bg-white"
                >
                  Copy
                </button>
              ) : null}
            </dd>
          </div>
        </dl>
        <div className="border-t border-slate-200 pt-3 space-y-2">
          <div className="flex items-center justify-between gap-2">
            <h4 className="text-xs font-semibold text-slate-700">SOAP report pkeys</h4>
            <button
              type="button"
              onClick={() => { if (!isDemoMode) void loadSoapPkeys(); }}
              className="text-xs font-medium text-sky-700 hover:underline"
            >
              Refresh
            </button>
          </div>
          {loadingSoapPkeys && <p className="text-xs text-slate-500">Loading…</p>}
          {soapPkeysError && <p className="text-xs text-amber-700">{soapPkeysError}</p>}
          {!loadingSoapPkeys && soapPkeys && soapPkeys.length === 0 && (
            <p className="text-xs text-slate-500">No rows in ReportQueryDefinitions.</p>
          )}
          {!loadingSoapPkeys && soapPkeys && soapPkeys.length > 0 && (
            <ul className="max-h-40 overflow-auto rounded border border-slate-200 bg-white divide-y divide-slate-100">
              {soapPkeys.map((k) => (
                <li key={k} className="flex items-center justify-between gap-2 px-2 py-1.5 text-xs font-mono text-slate-800">
                  <span className="break-all">{k}</span>
                  <button
                    type="button"
                    onClick={() => void copyToClipboard(k)}
                    className="shrink-0 text-sky-700 hover:underline"
                  >
                    Copy
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      </section>

      <section className="rounded-lg border-2 border-rose-300 bg-rose-50 p-4 space-y-3">
        <h3 className="text-sm font-semibold text-rose-900">Danger zone</h3>
        <p className="text-xs text-rose-700">
          These actions permanently delete data. Each requires confirmation before it runs.
        </p>

        <div className="grid gap-4 md:grid-cols-2">
          <div className="rounded-lg border border-rose-200 bg-white p-4 space-y-3">
            <h4 className="text-sm font-semibold text-slate-800">Reset patients</h4>
            <p className="text-xs text-slate-500">
              Truncate all patient-related tables. Use Generate patients (Data Generation tab) to
              repopulate with a consistent dataset.
            </p>
            <ConfirmableResetButton
              label="Reset patient data"
              confirming={resetConfirming}
              loading={loadingReset}
              isDemoMode={isDemoMode}
              onStart={() => setResetConfirming(true)}
              onConfirm={() => void handleReset()}
              onCancel={() => setResetConfirming(false)}
            />
          </div>

          <div className="rounded-lg border border-rose-200 bg-white p-4 space-y-3">
            <h4 className="text-sm font-semibold text-slate-800">Reset study data</h4>
            <p className="text-xs text-slate-500">
              Truncate all Study-domain tables. Use Generate studies (Data Generation tab) to
              repopulate with a consistent dataset. Patient data is untouched.
            </p>
            <ConfirmableResetButton
              label="Reset study data"
              confirming={resetStudiesConfirming}
              loading={loadingResetStudies}
              isDemoMode={isDemoMode}
              onStart={() => setResetStudiesConfirming(true)}
              onConfirm={() => void handleResetStudies()}
              onCancel={() => setResetStudiesConfirming(false)}
            />
          </div>
        </div>
      </section>
    </div>
  );
};

export default TestDataInfoDestructionSection;
