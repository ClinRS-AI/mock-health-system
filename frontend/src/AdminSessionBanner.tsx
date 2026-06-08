import React from "react";
import { useAdminSession } from "./AdminSessionContext";

/** Compact notice for admin-gated pages about current session state. */
const AdminSessionBanner: React.FC = () => {
  const { hasSession, expiresAtUtc, signOut } = useAdminSession();

  if (!hasSession || !expiresAtUtc) return null;

  const when = new Date(expiresAtUtc).toLocaleString(undefined, {
    dateStyle: "short",
    timeStyle: "short"
  });

  return (
    <div className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs text-emerald-900">
      <span>
        Admin session active until <span className="font-medium">{when}</span>.
      </span>
      <button
        type="button"
        onClick={() => signOut()}
        className="rounded border border-emerald-300 bg-white px-2 py-0.5 font-medium hover:bg-emerald-100"
      >
        Sign out
      </button>
    </div>
  );
};

export default AdminSessionBanner;
