import InteractiveStage from "../components/InteractiveStage";
import SyncSoundLogo from "../components/SyncSoundLogo";

const DOWNLOAD_EXE_URL =
  "https://github.com/theraghavkrishna/SYNC-SOUND/releases/download/v3.1.0/SYNC-SOUND-x64.exe";
const DOWNLOAD_ZIP_URL =
  "https://github.com/theraghavkrishna/SYNC-SOUND/releases/download/v3.1.0/SYNC-SOUND-Portable-x64.zip";
const GITHUB_URL = "https://github.com/theraghavkrishna/SYNC-SOUND";

export default function HomePage() {
  return (
    <div className="relative min-h-screen bg-[#F7F4ED] text-[#141417] overflow-x-hidden">
      {/* Subtle Warm Paper Grain */}
      <div className="pointer-events-none fixed inset-0 z-0 paper-grain opacity-75" />

      {/* Floating Cream Pill Navbar */}
      <header className="sticky top-4 z-40 px-4">
        <nav className="max-w-5xl mx-auto cream-card rounded-full px-4 py-2.5 flex items-center justify-between">
          <a href="#" className="flex items-center gap-3 pl-1">
            <SyncSoundLogo className="w-8 h-8 rounded-lg shadow-sm" />
            <span className="font-bold text-sm tracking-wide text-[#141417]">
              SYNC SOUND
            </span>
          </a>

          <div className="hidden md:flex items-center gap-7 text-xs font-semibold text-[#52525B]">
            <a href="#stage" className="hover:text-[#141417] transition-colors">
              Live Demo
            </a>
            <a href="#why" className="hover:text-[#141417] transition-colors">
              How It Works
            </a>
            <a
              href="#smartscreen"
              className="hover:text-[#141417] transition-colors"
            >
              First Launch
            </a>
            <a
              href={GITHUB_URL}
              target="_blank"
              rel="noreferrer"
              className="hover:text-[#141417] transition-colors"
            >
              GitHub
            </a>
          </div>

          <a
            href={DOWNLOAD_EXE_URL}
            className="px-4 py-2 rounded-full text-xs font-bold text-white transition-transform hover:scale-[1.03] active:scale-[0.97]"
            style={{
              background: "linear-gradient(135deg, #F06E51 0%, #D94B2D 100%)",
              boxShadow: "0 6px 18px -4px rgba(232,93,63,0.45)",
            }}
          >
            Download v3.1.0
          </a>
        </nav>
      </header>

      <main className="relative z-10 max-w-6xl mx-auto px-5 md:px-8 pt-16 pb-28 space-y-28">
        {/* HERO — YC-Level Crisp Editorial Copywriting */}
        <section className="text-center max-w-4xl mx-auto">
          <div className="inline-flex items-center gap-2.5 px-3.5 py-1.5 rounded-full bg-white border border-[#E4DFD3] shadow-xs mb-8">
            <span className="w-2 h-2 rounded-full bg-[#E85D3F]" />
            <span className="font-mono-tech text-[11px] uppercase tracking-widest text-[#52525B] font-medium">
              Windows 11 · v3.1.0 · Free &amp; Open Source
            </span>
          </div>

          <h1 className="text-5xl sm:text-7xl md:text-[84px] font-extrabold tracking-[-0.035em] leading-[0.95] text-[#141417]">
            Windows plays to one.
            <br />
            <span className="font-serif-editorial italic font-normal text-[#E85D3F]">
              Play to everyone.
            </span>
          </h1>

          <p className="mt-7 text-lg sm:text-xl text-[#52525B] max-w-2xl mx-auto leading-relaxed">
            Watch a movie on a flight. Listen to music together on one laptop.
            Pair any headphones in Windows, pick who hears, press one button.
          </p>

          {/* Primary Conversion Cluster */}
          <div className="mt-10 flex flex-col sm:flex-row items-center justify-center gap-4">
            <a
              href={DOWNLOAD_EXE_URL}
              className="w-full sm:w-auto px-8 py-4 rounded-full text-base font-bold text-white transition-all hover:scale-[1.03] active:scale-[0.97] inline-flex items-center justify-center gap-3"
              style={{
                background: "linear-gradient(135deg, #F06E51 0%, #D94B2D 100%)",
                boxShadow: "0 14px 36px -8px rgba(232,93,63,0.48)",
              }}
            >
              <span>Download for Windows 11</span>
              <span className="font-mono-tech text-xs px-2 py-0.5 rounded-full bg-black/15">
                .exe
              </span>
            </a>

            <a
              href={DOWNLOAD_ZIP_URL}
              className="w-full sm:w-auto px-7 py-4 rounded-full text-sm font-semibold text-[#141417] cream-card hover:border-[#E85D3F]/50 transition-colors"
            >
              Portable .zip
            </a>
          </div>

          <p className="mt-5 text-xs text-[#84848E]">
            Unsigned indie build — if SmartScreen appears, click{" "}
            <a
              href="#smartscreen"
              className="text-[#E85D3F] hover:underline font-semibold"
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

        {/* YC-STYLE SHARP VALUE PROPOSITION */}
        <section id="why" className="scroll-mt-24 space-y-12">
          <div className="max-w-2xl">
            <span className="font-mono-tech text-xs uppercase tracking-widest text-[#E85D3F] font-semibold">
              No Virtual Cables. No Audio Engineering.
            </span>
            <h2 className="text-3xl sm:text-5xl font-extrabold tracking-tight mt-3 leading-[1.05] text-[#141417]">
              Other tools give you a mixer board.
              <span className="font-serif-editorial italic font-normal text-[#52525B] block mt-1">
                We give you a switch.
              </span>
            </h2>
          </div>

          <div className="grid md:grid-cols-3 gap-5">
            <div className="cream-card rounded-[22px] p-7 space-y-3">
              <div className="w-10 h-10 rounded-xl bg-[#FDF0ED] border border-[#F4C2B5] flex items-center justify-center font-mono-tech text-xs font-bold text-[#E85D3F]">
                01
              </div>
              <h3 className="text-lg font-bold text-[#141417]">
                Individual Volume Knobs
              </h3>
              <p className="text-sm text-[#52525B] leading-relaxed">
                Your earbuds are loud; theirs are quiet. Turn each person&apos;s
                rotary dial independently without touching Windows settings.
              </p>
            </div>

            <div className="cream-card rounded-[22px] p-7 space-y-3">
              <div className="w-10 h-10 rounded-xl bg-[#FDF0ED] border border-[#F4C2B5] flex items-center justify-center font-mono-tech text-xs font-bold text-[#E85D3F]">
                02
              </div>
              <h3 className="text-lg font-bold text-[#141417]">
                Ctrl+Shift+S Quick Sidebar
              </h3>
              <p className="text-sm text-[#52525B] leading-relaxed">
                Never alt-tab out of Netflix or a game. Summon the floating
                sidebar from anywhere, adjust knobs, and tuck it right back.
              </p>
            </div>

            <div className="cream-card rounded-[22px] p-7 space-y-3">
              <div className="w-10 h-10 rounded-xl bg-[#FDF0ED] border border-[#F4C2B5] flex items-center justify-center font-mono-tech text-xs font-bold text-[#E85D3F]">
                03
              </div>
              <h3 className="text-lg font-bold text-[#141417]">
                Zero Bloat. Just Sharing.
              </h3>
              <p className="text-sm text-[#52525B] leading-relaxed">
                Three clean screens: Home, Devices, Settings. Plays YouTube,
                Spotify, Netflix, games — literally anything on your PC.
              </p>
            </div>
          </div>
        </section>

        {/* DARK INK-BLACK SMARTSCREEN TRUST SECTION */}
        <section id="smartscreen" className="scroll-mt-24">
          <div className="ink-card rounded-[28px] p-7 md:p-11 grid md:grid-cols-12 gap-8 items-center text-[#F7F4ED]">
            <div className="md:col-span-7 space-y-4">
              <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-[#E85D3F]/15 border border-[#E85D3F]/40 text-[#F06E51] text-xs font-semibold">
                First-Launch Windows Note
              </div>
              <h2 className="text-2xl sm:text-4xl font-extrabold tracking-tight">
                See &ldquo;Windows protected your PC&rdquo;?
              </h2>
              <p className="text-sm sm:text-base text-[#A1A1AA] leading-relaxed">
                SYNC SOUND is free, open-source, and published without a paid
                $400/yr Authenticode certificate. Windows SmartScreen flags
                every new indie `.exe` until it builds reputation.
              </p>
              <div className="flex flex-wrap gap-3 pt-2">
                <a
                  href={DOWNLOAD_EXE_URL}
                  className="px-6 py-3 rounded-full text-sm font-bold text-white"
                  style={{
                    background:
                      "linear-gradient(135deg, #F06E51 0%, #D94B2D 100%)",
                  }}
                >
                  Download v3.1.0.1 (.exe)
                </a>
                <a
                  href={GITHUB_URL}
                  target="_blank"
                  rel="noreferrer"
                  className="px-6 py-3 rounded-full text-sm font-semibold border border-white/15 hover:border-white/35 transition-colors"
                >
                  Inspect Source on GitHub
                </a>
              </div>
            </div>

            <div className="md:col-span-5 space-y-3">
              <div className="rounded-2xl p-4 bg-white/[0.05] border border-white/10 flex items-start gap-4">
                <span className="font-mono-tech text-xs font-bold px-2.5 py-1 rounded-lg bg-[#E85D3F]/20 text-[#F06E51] border border-[#E85D3F]/35">
                  STEP 1
                </span>
                <div>
                  <div className="font-semibold text-sm text-[#F7F4ED]">
                    Click &ldquo;More info&rdquo;
                  </div>
                  <div className="text-xs text-[#A1A1AA] mt-0.5">
                    Right under the SmartScreen message on the left.
                  </div>
                </div>
              </div>

              <div className="rounded-2xl p-4 bg-white/[0.05] border border-white/10 flex items-start gap-4">
                <span className="font-mono-tech text-xs font-bold px-2.5 py-1 rounded-lg bg-[#34D399]/15 text-[#34D399] border border-[#34D399]/30">
                  STEP 2
                </span>
                <div>
                  <div className="font-semibold text-sm text-[#F7F4ED]">
                    Click &ldquo;Run anyway&rdquo;
                  </div>
                  <div className="text-xs text-[#A1A1AA] mt-0.5">
                    Only needed once. Every future launch opens instantly.
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        {/* FINAL CTA */}
        <section className="text-center cream-card rounded-[28px] py-16 px-6">
          <SyncSoundLogo className="w-16 h-16 rounded-2xl mx-auto mb-6 shadow-xl" />
          <h2 className="text-3xl sm:text-5xl font-extrabold tracking-tight text-[#141417]">
            Connect. Select everyone.{" "}
            <span className="font-serif-editorial italic font-normal text-[#E85D3F]">
              Play.
            </span>
          </h2>
          <p className="mt-4 text-[#52525B] max-w-md mx-auto text-sm sm:text-base">
            Single-file Windows 11 executable. No account, no subscription, no
            audio routing headaches.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-4">
            <a
              href={DOWNLOAD_EXE_URL}
              className="px-8 py-4 rounded-full text-sm font-bold text-white transition-transform hover:scale-[1.03]"
              style={{
                background: "linear-gradient(135deg, #F06E51 0%, #D94B2D 100%)",
                boxShadow: "0 12px 30px -6px rgba(232,93,63,0.45)",
              }}
            >
              Download SYNC SOUND v3.1.0
            </a>
          </div>
        </section>
      </main>

      <footer className="relative z-10 border-t border-[#E4DFD3] py-8 px-6 text-center text-xs text-[#84848E]">
        <div className="max-w-6xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-4">
          <span>SYNC SOUND · Windows 11</span>
          <div className="flex items-center gap-6">
            <a href={DOWNLOAD_EXE_URL} className="hover:text-[#141417]">
              v3.1.0 EXE
            </a>
            <a href={DOWNLOAD_ZIP_URL} className="hover:text-[#141417]">
              Portable ZIP
            </a>
            <a
              href={GITHUB_URL}
              target="_blank"
              rel="noreferrer"
              className="hover:text-[#141417]"
            >
              GitHub
            </a>
          </div>
        </div>
      </footer>
    </div>
  );
}
