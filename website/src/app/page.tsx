import InteractiveStage from "../components/InteractiveStage";

const DOWNLOAD_EXE_URL =
  "https://github.com/theraghavkrishna/SYNC-SOUND/releases/download/v2.0.0/SYNC-SOUND-x64.exe";
const DOWNLOAD_ZIP_URL =
  "https://github.com/theraghavkrishna/SYNC-SOUND/releases/download/v2.0.0/SYNC-SOUND-Portable-x64.zip";
const GITHUB_URL = "https://github.com/theraghavkrishna/SYNC-SOUND";

export default function HomePage() {
  return (
    <div className="relative min-h-screen bg-[#07090F] text-[#F8FAFC] overflow-x-hidden">
      {/* Ambient Radial Lighting + Subtle Film Grain */}
      <div
        className="pointer-events-none fixed inset-0 z-0"
        style={{
          background:
            "radial-gradient(ellipse 75% 45% at 50% -5%, rgba(34,211,238,0.16), transparent 70%), radial-gradient(ellipse 50% 35% at 85% 55%, rgba(34,211,238,0.06), transparent 70%)",
        }}
      />
      <div className="pointer-events-none fixed inset-0 z-0 noise-overlay opacity-70" />

      {/* Floating Glass Pill Navbar */}
      <header className="sticky top-4 z-40 px-4">
        <nav className="max-w-5xl mx-auto glass-card rounded-full px-4 py-2.5 flex items-center justify-between">
          <a href="#" className="flex items-center gap-3 pl-1">
            <img
              src="/app-icon.png"
              alt="SYNC SOUND"
              className="w-8 h-8 rounded-lg shadow-md"
            />
            <span className="font-display font-bold text-sm tracking-wide">
              SYNC SOUND
            </span>
          </a>

          <div className="hidden md:flex items-center gap-7 text-xs font-medium text-[#94A3B8]">
            <a href="#stage" className="hover:text-[#F8FAFC] transition-colors">
              Live Stage
            </a>
            <a href="#craft" className="hover:text-[#F8FAFC] transition-colors">
              Architecture
            </a>
            <a
              href="#smartscreen"
              className="hover:text-[#F8FAFC] transition-colors"
            >
              First Launch
            </a>
            <a
              href={GITHUB_URL}
              target="_blank"
              rel="noreferrer"
              className="hover:text-[#F8FAFC] transition-colors"
            >
              GitHub
            </a>
          </div>

          <a
            href={DOWNLOAD_EXE_URL}
            className="px-4 py-2 rounded-full text-xs font-bold text-[#06121A] transition-transform hover:scale-[1.03] active:scale-[0.97]"
            style={{
              background: "linear-gradient(135deg, #38E8FF 0%, #06B6D4 100%)",
              boxShadow: "0 6px 20px -4px rgba(34,211,238,0.45)",
            }}
          >
            Download v2.0.0
          </a>
        </nav>
      </header>

      <main className="relative z-10 max-w-6xl mx-auto px-5 md:px-8 pt-16 pb-28 space-y-28">
        {/* HERO SECTION */}
        <section className="text-center max-w-4xl mx-auto">
          <div className="inline-flex items-center gap-2.5 px-3.5 py-1.5 rounded-full glass-card mb-8">
            <span className="w-2 h-2 rounded-full bg-[#22D3EE] shadow-[0_0_8px_#22D3EE]" />
            <span className="font-mono-tech text-[11px] uppercase tracking-widest text-[#94A3B8]">
              Windows 11 · v2.0.0 · Free &amp; Open Source
            </span>
          </div>

          <h1 className="font-display text-5xl sm:text-7xl md:text-[82px] font-extrabold tracking-[-0.03em] leading-[0.95] text-[#F8FAFC]">
            One sound.
            <br />
            <span
              className="bg-clip-text text-transparent"
              style={{
                backgroundImage:
                  "linear-gradient(135deg, #F8FAFC 10%, #22D3EE 90%)",
              }}
            >
              Everyone listening.
            </span>
          </h1>

          <p className="mt-7 text-lg sm:text-xl text-[#94A3B8] max-w-2xl mx-auto leading-relaxed">
            Pair two or more headphones to your Windows laptop. Pick who hears.
            Press one button. Zero audio-routing jargon.
          </p>

          {/* Primary Conversion Cluster */}
          <div className="mt-10 flex flex-col sm:flex-row items-center justify-center gap-4">
            <a
              href={DOWNLOAD_EXE_URL}
              className="w-full sm:w-auto px-8 py-4 rounded-full text-base font-bold text-[#06121A] transition-all hover:scale-[1.03] active:scale-[0.97] inline-flex items-center justify-center gap-3"
              style={{
                background: "linear-gradient(135deg, #38E8FF 0%, #06B6D4 100%)",
                boxShadow: "0 14px 40px -8px rgba(34,211,238,0.55)",
              }}
            >
              <span>Download for Windows 11</span>
              <span className="font-mono-tech text-xs px-2 py-0.5 rounded-full bg-[#06121A]/15">
                .exe
              </span>
            </a>

            <a
              href={DOWNLOAD_ZIP_URL}
              className="w-full sm:w-auto px-7 py-4 rounded-full text-sm font-semibold text-[#F8FAFC] glass-card hover:border-[#22D3EE]/40 transition-colors"
            >
              Portable .zip
            </a>
          </div>

          {/* Inline SmartScreen Quick Note */}
          <p className="mt-5 text-xs text-[#64748B]">
            Unsigned indie build — if Windows SmartScreen appears, click{" "}
            <a
              href="#smartscreen"
              className="text-[#22D3EE] hover:underline font-medium"
            >
              More info → Run anyway
            </a>
            .
          </p>
        </section>

        {/* INTERACTIVE SPATIAL STAGE */}
        <section id="stage" className="scroll-mt-24">
          <InteractiveStage />
        </section>

        {/* EDITORIAL ARCHITECTURE / WHY IT EXISTS */}
        <section id="craft" className="scroll-mt-24 space-y-10">
          <div className="max-w-2xl">
            <span className="font-mono-tech text-xs uppercase tracking-widest text-[#22D3EE]">
              Invisible Engineering
            </span>
            <h2 className="font-display text-3xl sm:text-5xl font-bold tracking-tight mt-3 leading-tight">
              Built like an instrument.
              <br />
              <span className="text-[#94A3B8]">Simple like a switch.</span>
            </h2>
          </div>

          <div className="grid md:grid-cols-3 gap-5">
            <div className="glass-card rounded-[22px] p-7 space-y-4">
              <div className="w-11 h-11 rounded-2xl bg-[#22D3EE]/10 border border-[#22D3EE]/25 flex items-center justify-center font-mono-tech text-sm font-bold text-[#22D3EE]">
                01
              </div>
              <h3 className="font-display text-xl font-bold">
                Per-Headphone Rotary Volume
              </h3>
              <p className="text-sm text-[#94A3B8] leading-relaxed">
                Different earbuds have wildly different sensitivity. Dial each
                listener&apos;s level independently with tactile rotary knobs
                right in Devices or the Quick Sidebar.
              </p>
            </div>

            <div className="glass-card rounded-[22px] p-7 space-y-4">
              <div className="w-11 h-11 rounded-2xl bg-[#22D3EE]/10 border border-[#22D3EE]/25 flex items-center justify-center font-mono-tech text-sm font-bold text-[#22D3EE]">
                02
              </div>
              <h3 className="font-display text-xl font-bold">
                Instant Quick Sidebar
              </h3>
              <p className="text-sm text-[#94A3B8] leading-relaxed">
                Hit{" "}
                <span className="font-mono-tech text-[#F8FAFC] px-1.5 py-0.5 rounded bg-white/10">
                  Ctrl+Shift+S
                </span>{" "}
                over any movie, game, or browser tab. Toggle listeners or tweak
                knobs without leaving full screen.
              </p>
            </div>

            <div className="glass-card rounded-[22px] p-7 space-y-4">
              <div className="w-11 h-11 rounded-2xl bg-[#22D3EE]/10 border border-[#22D3EE]/25 flex items-center justify-center font-mono-tech text-sm font-bold text-[#22D3EE]">
                03
              </div>
              <h3 className="font-display text-xl font-bold">
                Native WASAPI Fan-Out
              </h3>
              <p className="text-sm text-[#94A3B8] leading-relaxed">
                Captures system audio cleanly via WASAPI loopback and feeds
                every selected output with automatic resampling and disconnect
                isolation.
              </p>
            </div>
          </div>
        </section>

        {/* SMARTSCREEN FIRST-LAUNCH TRUST GUIDE */}
        <section id="smartscreen" className="scroll-mt-24">
          <div className="glass-card-active rounded-[26px] p-7 md:p-10 grid md:grid-cols-12 gap-8 items-center">
            <div className="md:col-span-7 space-y-4">
              <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-[#FBBF24]/10 border border-[#FBBF24]/30 text-[#FBBF24] text-xs font-semibold">
                First-Launch Windows Note
              </div>
              <h2 className="font-display text-2xl sm:text-4xl font-bold tracking-tight">
                See &ldquo;Windows protected your PC&rdquo;?
              </h2>
              <p className="text-sm sm:text-base text-[#94A3B8] leading-relaxed">
                SYNC SOUND is free, open-source, and published without a paid
                $400/yr Authenticode certificate. Windows SmartScreen flags
                all new unsigned indie apps until they build reputation.
              </p>
              <div className="flex flex-wrap gap-3 pt-2">
                <a
                  href={DOWNLOAD_EXE_URL}
                  className="px-6 py-3 rounded-full text-sm font-bold text-[#06121A]"
                  style={{
                    background:
                      "linear-gradient(135deg, #38E8FF 0%, #06B6D4 100%)",
                  }}
                >
                  Download v2.0.0 (.exe)
                </a>
                <a
                  href={GITHUB_URL}
                  target="_blank"
                  rel="noreferrer"
                  className="px-6 py-3 rounded-full text-sm font-semibold glass-card hover:border-white/25 transition-colors"
                >
                  Inspect Source on GitHub
                </a>
              </div>
            </div>

            <div className="md:col-span-5 space-y-3">
              <div className="glass-card rounded-2xl p-4 flex items-start gap-4">
                <span className="font-mono-tech text-xs font-bold px-2.5 py-1 rounded-lg bg-[#22D3EE]/15 text-[#22D3EE] border border-[#22D3EE]/30">
                  STEP 1
                </span>
                <div>
                  <div className="font-semibold text-sm">
                    Click &ldquo;More info&rdquo;
                  </div>
                  <div className="text-xs text-[#94A3B8] mt-0.5">
                    Look just under the SmartScreen message on the left side.
                  </div>
                </div>
              </div>

              <div className="glass-card rounded-2xl p-4 flex items-start gap-4">
                <span className="font-mono-tech text-xs font-bold px-2.5 py-1 rounded-lg bg-[#34D399]/15 text-[#34D399] border border-[#34D399]/30">
                  STEP 2
                </span>
                <div>
                  <div className="font-semibold text-sm">
                    Click &ldquo;Run anyway&rdquo;
                  </div>
                  <div className="text-xs text-[#94A3B8] mt-0.5">
                    Windows remembers your choice—every future launch opens
                    instantly.
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        {/* FINAL CTA */}
        <section className="text-center glass-card rounded-[28px] py-16 px-6">
          <img
            src="/app-icon.png"
            alt="SYNC SOUND App Icon"
            className="w-16 h-16 rounded-2xl mx-auto mb-6 shadow-2xl"
          />
          <h2 className="font-display text-3xl sm:text-5xl font-bold tracking-tight">
            Ready to share the sound?
          </h2>
          <p className="mt-4 text-[#94A3B8] max-w-md mx-auto text-sm sm:text-base">
            Single-file Windows 11 executable. No account, no subscription, no
            audio clutter.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-4">
            <a
              href={DOWNLOAD_EXE_URL}
              className="px-8 py-4 rounded-full text-sm font-bold text-[#06121A] transition-transform hover:scale-[1.03]"
              style={{
                background: "linear-gradient(135deg, #38E8FF 0%, #06B6D4 100%)",
                boxShadow: "0 12px 35px -6px rgba(34,211,238,0.5)",
              }}
            >
              Download SYNC SOUND v2.0.0
            </a>
          </div>
        </section>
      </main>

      <footer className="relative z-10 border-t border-white/[0.06] py-8 px-6 text-center text-xs text-[#64748B]">
        <div className="max-w-6xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-4">
          <span>SYNC SOUND · Windows 11</span>
          <div className="flex items-center gap-6">
            <a href={DOWNLOAD_EXE_URL} className="hover:text-[#F8FAFC]">
              v2.0.0 EXE
            </a>
            <a href={DOWNLOAD_ZIP_URL} className="hover:text-[#F8FAFC]">
              Portable ZIP
            </a>
            <a
              href={GITHUB_URL}
              target="_blank"
              rel="noreferrer"
              className="hover:text-[#F8FAFC]"
            >
              GitHub
            </a>
          </div>
        </div>
      </footer>
    </div>
  );
}
