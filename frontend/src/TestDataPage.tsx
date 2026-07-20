import React, { useState } from "react";
import AdminSessionBanner from "./AdminSessionBanner";
import TestDataCountsSection from "./TestDataCountsSection";
import TestDataGenerationSection from "./TestDataGenerationSection";
import TestDataManipulationSection from "./TestDataManipulationSection";
import TestDataInfoDestructionSection from "./TestDataInfoDestructionSection";

type TestDataTab = "counts" | "generation" | "manipulation" | "info";

const TABS: { key: TestDataTab; label: string }[] = [
  { key: "counts", label: "Data Counts and Visualizations" },
  { key: "generation", label: "Data Generation" },
  { key: "manipulation", label: "Data Manipulation" },
  { key: "info", label: "Information and Destruction" }
];

const TestDataPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<TestDataTab>("counts");
  const [manipulationHasUnsavedEdit, setManipulationHasUnsavedEdit] = useState(false);

  function handleTabChange(nextTab: TestDataTab) {
    if (activeTab === "manipulation" && nextTab !== "manipulation" && manipulationHasUnsavedEdit) {
      const confirmed = window.confirm(
        "You have an unsaved patient record edit. Switching tabs will discard it. Continue?"
      );
      if (!confirmed) return;
    }
    setActiveTab(nextTab);
  }

  return (
    <div className="space-y-4">
      <AdminSessionBanner />

      <nav className="flex flex-wrap gap-3 border-b border-slate-100 pb-3 text-sm">
        {TABS.map((tab) => (
          <button
            key={tab.key}
            type="button"
            className={`px-3 py-1.5 rounded-md ${
              activeTab === tab.key
                ? "bg-sky-50 text-sky-700 border border-sky-200"
                : "text-slate-600 hover:bg-slate-50 border border-transparent"
            }`}
            onClick={() => handleTabChange(tab.key)}
          >
            {tab.label}
          </button>
        ))}
      </nav>

      {/* Conditional (unmount/remount) rendering, not CSS show/hide: switching back to "counts"
          re-fetches fresh stats on mount, which is also what makes Generation/Info's mutation
          handlers not needing an explicit post-mutation stats refresh — they rely on this
          remount rather than calling a stats loader directly. If this ever changes to
          keep sections mounted (e.g. to preserve in-progress state across tabs), an explicit
          refresh call/shared invalidation signal would need to be reintroduced. */}
      {activeTab === "counts" && <TestDataCountsSection />}
      {activeTab === "generation" && <TestDataGenerationSection />}
      {activeTab === "manipulation" && (
        <TestDataManipulationSection onEditingChange={setManipulationHasUnsavedEdit} />
      )}
      {activeTab === "info" && <TestDataInfoDestructionSection />}
    </div>
  );
};

export default TestDataPage;
