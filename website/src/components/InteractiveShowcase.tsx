"use client";

import { useState } from "react";

type Tab = "home" | "devices" | "player";

interface DemoDevice {
  id: string;
  name: string;
  category: string;
  selected: boolean;
  isDefault?: boolean;
}

const INITIAL_DEVICES: DemoDevice[] = [
  { id: "1", name: "OnePlus Nord Buds 3 Pro", category: "Headphones", selected: true, isDefault: true },
  { id: "2", name: "AirPods Pro", category: "Headphones", selected: true },
  { id: "3", name: "Galaxy Buds2 Pro", category: "Headphones", selected: true },
  { id: "4", name: "Laptop Speakers", category: "Speakers", selected: false },
];

export default function InteractiveShowcase() {
  const [tab, setTab] = useState<Tab>("home");
  const [devices, setDevices] = useState<DemoDevice[]>(INITIAL_DEVICES);
  const [isSharing, setIsSharing] = useState(false);
  const [sidebarOpen, setSidebarOpen] = useState(true);

  const selectedDevices = devices.filter((d) => d.selected);
  const receiverCount = Math.max(0, selectedDevices.filter((d) => !d.isDefault).length);

  const toggleDevice = (id: string) => {
    setDevices((prev) => prev.map((d) => (d.id === id ? { ...d, selected: !d.selected } : d)));
  };

  return (
    <div className="showcase-wrap">
      <div style={{ display: "flex", justifyContent: "flex-end", marginBottom: 12, gap: 10, alignItems: "center" }}>
        <span style={{ fontSize: 12.5, color: "var(--ink-muted)" }}>Interactive preview · Try clicking around</span>
        <button
          type="button"
          className="btn-secondary"
          style={{ padding: "7px 14px", fontSize: 12.5 }}
          onClick={() => setSidebarOpen((v) => !v)}
        >
          {sidebarOpen ? "Hide" : "Show"} Quick Sidebar <span className="kbd">Ctrl+Shift+S</span>
        </button>
      </div>

      <div className="app-frame">
        <aside className="app-rail">
          <div>
            <div className="brand">
              <span className="wave-mark" aria-hidden="true">
                <span className="wave-bar" style={{ height: 10 }} />
                <span className="wave-bar" style={{ height: 18 }} />
                <span className="wave-bar dark" style={{ height: 13 }} />
              </span>
              <span>SYNC SOUND</span>
            </div>
            <div style={{ fontSize: 11.5, color: "var(--ink-muted)", marginTop: 4 }}>
              One sound. Everyone listening.
            </div>

            <nav className="rail-nav" aria-label="App preview navigation">
              <button
                type="button"
                className={`rail-item ${tab === "home" ? "active" : ""}`}
                onClick={() => setTab("home")}
              >
                Home
              </button>
              <button
                type="button"
                className={`rail-item ${tab === "devices" ? "active" : ""}`}
                onClick={() => setTab("devices")}
              >
                Devices
              </button>
              <button
                type="button"
                className={`rail-item ${tab === "player" ? "active" : ""}`}
                onClick={() => setTab("player")}
              >
                Player
              </button>
            </nav>
          </div>

          <div className="rail-footer">
            <div style={{ display: "flex", alignItems: "center", gap: 8, fontWeight: 600 }}>
              <span className="dot-green" />
              <span>{devices.length} devices connected</span>
            </div>
            <div style={{ color: "var(--ink-muted)", fontSize: 11.5, marginTop: 2, paddingLeft: 15 }}>
              Running in background
            </div>
          </div>
        </aside>

        <div className="app-stage">
          {tab === "home" && (
            <div className="stage-center">
              <div className={`emblem-circle ${isSharing ? "sharing" : ""}`}>
                <span className="wave-mark" aria-hidden="true">
                  <span className="wave-bar" style={{ width: 4.5, height: 14 }} />
                  <span className="wave-bar" style={{ width: 4.5, height: 26 }} />
                  <span className="wave-bar dark" style={{ width: 4.5, height: 19 }} />
                  <span className="wave-bar" style={{ width: 4.5, height: 12 }} />
                </span>
              </div>

              <h3 style={{ fontSize: 30, fontWeight: 650, letterSpacing: "-0.025em", marginBottom: 8 }}>
                {isSharing ? "You're all listening." : "Ready to listen."}
              </h3>
              <p style={{ fontSize: 14.5, color: "var(--ink-secondary)" }}>
                {isSharing
                  ? `Playing normally on your default earbuds while ${receiverCount} additional headphone(s) listen live.`
                  : `${selectedDevices.length} devices selected. Play anything on Windows and everyone hears it.`}
              </p>

              <div className="device-chips">
                {selectedDevices.map((d) => (
                  <span key={d.id} className="chip">
                    <span className="dot-green" />
                    {d.name}
                  </span>
                ))}
              </div>

              <div style={{ display: "flex", justifyContent: "center", gap: 10, flexWrap: "wrap" }}>
                <button type="button" className="btn-primary" onClick={() => setIsSharing((s) => !s)}>
                  {isSharing ? "Stop sharing" : "Start sharing"}
                </button>
                <button type="button" className="btn-secondary" onClick={() => setTab("devices")}>
                  Choose headphones
                </button>
              </div>
            </div>
          )}

          {tab === "devices" && (
            <div>
              <div style={{ marginBottom: 18 }}>
                <h3 style={{ fontSize: 22, fontWeight: 650 }}>Your headphones</h3>
                <p style={{ fontSize: 13.5, color: "var(--ink-secondary)" }}>
                  {selectedDevices.length} selected · {devices.length} connected
                </p>
              </div>
              <div className="device-list">
                {devices.map((d) => (
                  <div key={d.id} className="device-row">
                    <div>
                      <div style={{ fontWeight: 600, fontSize: 14.5 }}>
                        {d.name}
                        {d.isDefault && (
                          <span
                            style={{
                              marginLeft: 8,
                              fontSize: 11,
                              background: "var(--coral-soft)",
                              color: "var(--coral)",
                              padding: "2px 8px",
                              borderRadius: 999,
                              fontWeight: 600,
                            }}
                          >
                            Playing original sound
                          </span>
                        )}
                      </div>
                      <div style={{ fontSize: 12.5, color: "var(--ink-muted)" }}>
                        Connected · {d.category}
                      </div>
                    </div>
                    <button
                      type="button"
                      aria-label={`Toggle ${d.name}`}
                      className={`toggle ${d.selected ? "on" : ""}`}
                      onClick={() => toggleDevice(d.id)}
                    />
                  </div>
                ))}
              </div>
            </div>
          )}

          {tab === "player" && (
            <div style={{ textAlign: "center", maxWidth: 360, margin: "0 auto" }}>
              <div
                style={{
                  width: 156,
                  height: 156,
                  margin: "0 auto 18px",
                  borderRadius: 20,
                  background: "linear-gradient(135deg, #fdf0ed 0%, #f5dfd8 100%)",
                  border: "1px solid var(--coral-border)",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                  boxShadow: "0 18px 40px rgba(232,93,63,0.16)",
                }}
              >
                <span className="wave-mark">
                  <span className="wave-bar" style={{ width: 6, height: 22 }} />
                  <span className="wave-bar" style={{ width: 6, height: 42 }} />
                  <span className="wave-bar dark" style={{ width: 6, height: 30 }} />
                  <span className="wave-bar" style={{ width: 6, height: 18 }} />
                </span>
              </div>
              <div style={{ fontSize: 20, fontWeight: 650 }}>Midnight City</div>
              <div style={{ fontSize: 13.5, color: "var(--ink-secondary)", marginBottom: 12 }}>
                Drag & drop MP3, WAV, FLAC, or M4A
              </div>
              <span
                style={{
                  display: "inline-block",
                  background: "var(--coral-soft)",
                  color: "var(--coral)",
                  fontSize: 12,
                  fontWeight: 600,
                  padding: "5px 12px",
                  borderRadius: 999,
                }}
              >
                Playing through {selectedDevices.length} devices
              </span>
            </div>
          )}
        </div>
      </div>

      {/* Floating Quick Sidebar Card */}
      <div className={`floating-sidebar ${sidebarOpen ? "" : "hidden"}`}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 12 }}>
          <div className="brand" style={{ fontSize: 13 }}>
            <span className="wave-mark" aria-hidden="true">
              <span className="wave-bar" style={{ width: 3, height: 9 }} />
              <span className="wave-bar" style={{ width: 3, height: 15 }} />
              <span className="wave-bar dark" style={{ width: 3, height: 11 }} />
            </span>
            <span>SYNC SOUND</span>
          </div>
          <button
            type="button"
            onClick={() => setSidebarOpen(false)}
            style={{ border: "none", background: "transparent", color: "var(--ink-muted)", fontSize: 18 }}
            aria-label="Close quick sidebar preview"
          >
            ×
          </button>
        </div>

        <div
          style={{
            background: "var(--surface)",
            border: "1px solid var(--border)",
            borderRadius: 16,
            padding: 14,
            marginBottom: 10,
          }}
        >
          <div style={{ fontWeight: 650, fontSize: 16, marginBottom: 2 }}>
            {isSharing ? "You're all listening." : "Ready to listen."}
          </div>
          <div style={{ fontSize: 12, color: "var(--ink-secondary)", marginBottom: 12 }}>
            {selectedDevices.length} selected · Ctrl+Shift+S
          </div>
          <button
            type="button"
            className="btn-primary sm"
            style={{ width: "100%" }}
            onClick={() => setIsSharing((s) => !s)}
          >
            {isSharing ? "Stop sharing" : "Start sharing"}
          </button>
        </div>

        <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
          {devices.slice(0, 3).map((d) => (
            <div
              key={d.id}
              style={{
                background: "var(--surface)",
                border: "1px solid var(--border)",
                borderRadius: 12,
                padding: "8px 11px",
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                fontSize: 12.5,
                fontWeight: 500,
              }}
            >
              <span style={{ overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap", maxWidth: 180 }}>
                {d.name}
              </span>
              <button
                type="button"
                aria-label={`Sidebar toggle ${d.name}`}
                className={`toggle ${d.selected ? "on" : ""}`}
                style={{ transform: "scale(0.82)", transformOrigin: "right center" }}
                onClick={() => toggleDevice(d.id)}
              />
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
