import React from "react";

interface DemoBannerProps {
  onNavigateToAdmin: () => void;
}

export function DemoBanner({ onNavigateToAdmin }: DemoBannerProps): React.JSX.Element {
  return (
    <div className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      <div className="space-y-1">
        <p className="text-sm font-semibold text-amber-900">Demo Mode</p>
        <p className="text-sm text-amber-800">
          You are viewing read-only demo data. To interact with the real system, sign in on the
          Admin access tab.
        </p>
      </div>
      <button
        type="button"
        onClick={onNavigateToAdmin}
        className="w-full sm:w-auto shrink-0 inline-flex items-center justify-center rounded-md border border-amber-300 bg-white px-3 py-1.5 text-sm font-medium text-amber-900 hover:bg-amber-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-amber-500"
      >
        Go to Admin access
      </button>
    </div>
  );
}

export default DemoBanner;
