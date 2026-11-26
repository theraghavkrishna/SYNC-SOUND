import InteractiveStage from "../components/InteractiveStage";
import SyncSoundLogo from "../components/SyncSoundLogo";
import { RELEASE } from "../lib/release";

export default function HomePage() {
  return (
    <div className="relative min-h-screen bg-[#F4EFE6] text-[#191817] overflow-x-hidden">
      {/* Sticky Minimal Navigation */}
      <header className="sticky top-4 z-40 px-4">
        <nav className="max-w-5xl mx-auto cream-card rounded-full px-4 py-2.5 flex items-center justify-between">
          <a href="#" className="flex items-center gap-3 pl-1">
            <SyncSoundLogo className="w-8 h-8 rounded-lg shadow-xs" />
            <span className="font-bold text-sm tracking-tight text-[#191817]">
              SYNC SOUND
            </span>
          </a>

          <div className="hidden md:flex items-center gap-7 text-xs font-semibold text-[#595550]">
            <a href="#preview" className="hover:text-[#191817] transition-colors">
              Preview
            </a>
            <a href="#how-it-works" className="hover:text-[#191817] transition-colors">
              How it works
            </a>
            <a href="#compatibility" className="hover:text-[#191817] transition-colors">
              Compatibility
            </a>
            <a href="#download" className="hover:text-[#191817] transition-colors">
              Release {RELEASE.tag}
            </a>
            <a
              href={RELEASE.githubUrl}
              target="_blank"
              rel="noreferrer"
              className="hover:text-[#191817] transition-colors"
            >
              GitHub
            </a>
          </div>

          <a
            href={RELEASE.exe.url}
            className="px-4 py-2 rounded-full text-xs font-semibold text-[#FCFBF8] bg-[#C65D32] hover:bg-[#D56A3E] shadow-[0_6px_18px_-4px_rgba(198,93,50,0.45)] transition-transform hover:scale-[1.02] active:scale-[0.97]"
          >
            Download for Windows
          </a>
        </nav>
      </header>

      <main className="relative z-10 max-w-6xl mx-auto px-5 md:px-8 pt-14 pb-24 space-y-24">
        {/* HERO — Download-First, Clear Within 5 Seconds */}
        <section className="text-center max-w-4xl mx-auto">
          <div className="inline-flex items-center gap-2.5 px-3.5 py-1.5 rounded-full bg-[#FCFBF8] border border-[#DFD8CA] shadow-2xs mb-7">
            <span className="w-2 h-2 rounded-full bg-[#C65D32]" />
            <span className="font-mono-tech text-[11px] uppercase tracking-[0.14em] text-[#595550] font-semibold">
              The simple way to listen together
            </span>
          </div>

          <h1 className="text-5xl sm:text-7xl md:text-[80px] font-extrabold tracking-[-0.035em] leading-[0.96] text-[#191817]">
            Windows plays to one.
            <br />
            <span className="font-serif-editorial italic font-normal text-[#C65D32]">
              Play to everyone.
            </span>
          </h1>

          <p className="mt-6 text-lg sm:text-xl text-[#595550] max-w-2xl mx-auto leading-relaxed">
            Share your Windows PC&apos;s audio across multiple supported headphones.
            Choose who&apos;s listening. Press play.
          </p>

          <div className="mt-9 flex flex-col sm:flex-row items-center justify-center gap-3.5">
            <a
              href={RELEASE.exe.url}
              className="w-full sm:w-auto px-8 py-4 rounded-full text-base font-semibold text-[#FCFBF8] bg-[#C65D32] hover:bg-[#D56A3E] shadow-[0_14px_34px_-8px_rgba(198,93,50,0.48)] transition-all hover:scale-[1.02] active:scale-[0.97] inline-flex items-center justify-center gap-3"
            >
              <span>Download for Windows</span>
              <span className="font-mono-tech text-xs px-2 py-0.5 rounded-full bg-black/15">
                .exe
              </span>
            </a>

            <a
              href="#preview"
              className="w-full sm:w-auto px-7 py-4 rounded-full text-sm font-semibold text-[#191817] cream-card hover:border-[#C65D32]/50 transition-colors"
            >
              See it in action
            </a>
          </div>

          <p className="mt-5 font-mono-tech text-[11px] text-[#878179]">
            {RELEASE.architecture} · {RELEASE.tag} · {RELEASE.exe.sizeLabel} · Free &amp; Open Source · No account needed
          </p>
        </section>

        {/* INTERACTIVE PRODUCT PREVIEW */}
        <section id="preview" className="scroll-mt-24">
          <InteractiveStage />
        </section>

        {/* STORYTELLING — Short, Human Origin */}
        <section className="max-w-3xl mx-auto text-center space-y-4 py-4">
          <span className="font-mono-tech text-[11px] uppercase tracking-[0.16em] text-[#C65D32] font-semibold">
            Why We Built SYNC SOUND
          </span>
          <h2 className="text-3xl sm:text-5xl font-extrabold tracking-tight text-[#191817]">
            It started with a movie night.
          </h2>
          <p className="text-base sm:text-lg text-[#595550] leading-relaxed">
            Three friends. One laptop. Three pairs of headphones. Getting everyone to
            hear the same film on Windows meant digging through virtual audio cables,
            driver mixers, and broken workarounds. We just wanted a switch — so we built one.
          </p>
        </section>

        {/* HOW IT WORKS — CONNECT -> CHOOSE -> LISTEN */}
        <section id="how-it-works" className="scroll-mt-24 space-y-8">
          <div className="flex flex-col md:flex-row md:items-end justify-between gap-4">
            <div>
              <span className="font-mono-tech text-[11px] uppercase tracking-[0.16em] text-[#C65D32] font-semibold">
                Three Steps
              </span>
              <h2 className="text-3xl sm:text-4xl font-extrabold tracking-tight mt-2 text-[#191817]">
                Connect. Choose. Listen.
              </h2>
            </div>
            <p className="text-sm text-[#595550] max-w-md">
              Windows pairs your Bluetooth or wired gear. SYNC SOUND brings them into
              one listening group.
            </p>
          </div>

          <div className="grid md:grid-cols-3 gap-5">
            <div className="cream-card rounded-[22px] p-7 space-y-3">
              <div className="font-mono-tech text-xs font-bold text-[#C65D32]">
                01 · CONNECT
              </div>
              <h3 className="text-xl font-bold text-[#191817]">
                Pair your headphones with Windows
              </h3>
              <p className="text-sm text-[#595550] leading-relaxed">
                Connect Bluetooth earbuds, over-ear headphones, or wired headsets
                in Windows Settings. SYNC SOUND discovers them automatically.
              </p>
            </div>

            <div className="cream-card rounded-[22px] p-7 space-y-3">
              <div className="font-mono-tech text-xs font-bold text-[#C65D32]">
                02 · CHOOSE
              </div>
              <h3 className="text-xl font-bold text-[#191817]">
                Pick your listening group
              </h3>
              <p className="text-sm text-[#595550] leading-relaxed">
                Toggle who hears the sound and dial each listener&apos;s own volume
                knob independently.
              </p>
            </div>

            <div className="cream-card rounded-[22px] p-7 space-y-3">
              <div className="font-mono-tech text-xs font-bold text-[#C65D32]">
                03 · LISTEN
              </div>
              <h3 className="text-xl font-bold text-[#191817]">
                Press play on anything
              </h3>
              <p className="text-sm text-[#595550] leading-relaxed">
                Netflix, YouTube, Spotify, games — SYNC SOUND shares your
                Windows sound across the group.
              </p>
            </div>
          </div>
        </section>

        {/* CAPABILITIES — Asymmetric Editorial Layout */}
        <section className="grid md:grid-cols-2 gap-5">
          <div className="cream-card rounded-[24px] p-8 space-y-3">
            <span className="font-mono-tech text-[11px] uppercase tracking-[0.14em] text-[#C65D32] font-semibold">
              Listening Group Control
            </span>
            <h3 className="text-2xl font-bold text-[#191817]">
              Everyone gets their own level.
            </h3>
            <p className="text-sm text-[#595550] leading-relaxed">
              Your earbuds run loud while a friend&apos;s over-ear cans run quiet. Adjust
              the master Sync Dial or tune individual headphones without touching
              Windows mixer sliders.
            </p>
          </div>

          <div className="ink-card rounded-[24px] p-8 text-[#F4EFE6] space-y-3">
            <span className="font-mono-tech text-[11px] uppercase tracking-[0.14em] text-[#C65D32] font-semibold">
              Ctrl + Shift + S
            </span>
            <h3 className="text-2xl font-bold">
              Your sound, one shortcut away.
            </h3>
            <p className="text-sm text-[#B8B1A6] leading-relaxed">
              Summon the floating Quick Sidebar without leaving fullscreen video
              or a game. Start listening, mute, or switch devices in two seconds,
              then let it tuck right back out of sight.
            </p>
          </div>

          <div className="cream-card rounded-[24px] p-8 space-y-3">
            <span className="font-mono-tech text-[11px] uppercase tracking-[0.14em] text-[#C65D32] font-semibold">
              Fault Recovery
            </span>
            <h3 className="text-2xl font-bold text-[#191817]">
              One goes out? Everyone else stays in.
            </h3>
            <p className="text-sm text-[#595550] leading-relaxed">
              If someone walks out of Bluetooth range, healthy headphones keep
              listening uninterrupted. When they walk back in, SYNC SOUND brings
              them back automatically.
            </p>
          </div>

          <div className="cream-card rounded-[24px] p-8 space-y-3">
            <span className="font-mono-tech text-[11px] uppercase tracking-[0.14em] text-[#C65D32] font-semibold">
              Light · Dark · System
            </span>
            <h3 className="text-2xl font-bold text-[#191817]">
              Private, lightweight, theme-aware.
            </h3>
            <p className="text-sm text-[#595550] leading-relaxed">
              No account, no cloud, zero telemetry. Switch cleanly between warm
              Cream Light mode, Charcoal Dark mode, or follow your Windows setting.
            </p>
          </div>
        </section>

        {/* COMPATIBILITY & HONEST EXPECTATIONS */}
        <section id="compatibility" className="scroll-mt-24 cream-card rounded-[26px] p-7 md:p-10 space-y-6">
          <div className="max-w-2xl">
            <span className="font-mono-tech text-[11px] uppercase tracking-[0.14em] text-[#C65D32] font-semibold">
              Compatibility &amp; Honesty
            </span>
            <h2 className="text-2xl sm:text-3xl font-extrabold tracking-tight mt-2 text-[#191817]">
              What works best on Windows.
            </h2>
          </div>

          <div className="grid md:grid-cols-3 gap-6 text-sm text-[#595550]">
            <div className="space-y-1.5">
              <div className="font-bold text-[#191817]">Supported Systems &amp; Gear</div>
              <p className="leading-relaxed">
                Windows 10 &amp; 11 (64-bit). Works with Bluetooth A2DP headphones,
                earbuds, USB-C/3.5mm wired headsets, and external DACs.
              </p>
            </div>

            <div className="space-y-1.5">
              <div className="font-bold text-[#191817]">Wired 3.5 mm + Bluetooth TWS</div>
              <p className="leading-relaxed">
                Pair a 3.5 mm wired headset or USB-C DAC right alongside Bluetooth TWS earbuds.
                Realtek jack endpoints and wireless audio run together on one shared timeline.
              </p>
            </div>

            <div className="space-y-1.5">
              <div className="font-bold text-[#191817]">Bluetooth Latency &amp; Sync</div>
              <p className="leading-relaxed">
                Different Bluetooth models have slight hardware codec delay
                differences. SYNC SOUND protects against clock drift and includes
                optional per-device millisecond calibration in Preferences.
              </p>
            </div>
          </div>
        </section>

        {/* DOWNLOAD & VERIFICATION HUB */}
        <section id="download" className="scroll-mt-24 grid md:grid-cols-12 gap-5 items-stretch">
          <div className="md:col-span-7 cream-card rounded-[26px] p-7 md:p-9 flex flex-col justify-between space-y-6">
            <div className="space-y-2">
              <span className="font-mono-tech text-[11px] uppercase tracking-[0.14em] text-[#C65D32] font-semibold">
                Release {RELEASE.tag} · {RELEASE.date}
              </span>
              <h2 className="text-2xl sm:text-3xl font-extrabold text-[#191817]">
                Download for Windows
              </h2>
              <p className="text-sm text-[#595550]">
                Self-contained 64-bit Windows build — includes everything required to run out of the box.
              </p>
            </div>

            <div className="grid sm:grid-cols-2 gap-3.5">
              <a
                href={RELEASE.exe.url}
                className="p-4 rounded-2xl bg-[#C65D32] hover:bg-[#D56A3E] text-[#FCFBF8] transition-colors flex flex-col justify-between gap-2"
              >
                <div className="flex items-center justify-between">
                  <span className="font-bold text-sm">Windows Standalone (.exe)</span>
                  <span className="font-mono-tech text-[11px] px-2 py-0.5 rounded bg-black/15">
                    Recommended
                  </span>
                </div>
                <div className="font-mono-tech text-[11px] opacity-90">
                  {RELEASE.exe.filename} · {RELEASE.exe.sizeLabel}
                </div>
              </a>

              <a
                href={RELEASE.zip.url}
                className="p-4 rounded-2xl border border-[#DFD8CA] bg-[#F4EFE6]/60 hover:border-[#C65D32]/50 text-[#191817] transition-colors flex flex-col justify-between gap-2"
              >
                <div className="flex items-center justify-between">
                  <span className="font-bold text-sm">Portable Archive (.zip)</span>
                  <span className="font-mono-tech text-[11px] text-[#878179]">.zip</span>
                </div>
                <div className="font-mono-tech text-[11px] text-[#595550]">
                  {RELEASE.zip.filename} · {RELEASE.zip.sizeLabel}
                </div>
              </a>
            </div>

            <div className="pt-2 border-t border-[#DFD8CA] space-y-1">
              <div className="font-mono-tech text-[11px] text-[#595550]">
                SHA-256 (.exe): <span className="text-[#191817] break-all">{RELEASE.exe.sha256}</span>
              </div>
              <div className="text-xs text-[#878179]">
                <a href={RELEASE.releaseUrl} target="_blank" rel="noreferrer" className="text-[#C65D32] hover:underline font-semibold">
                  View release notes on GitHub
                </a>
              </div>
            </div>
          </div>

          <div className="md:col-span-5 cream-card rounded-[26px] p-7 md:p-9 flex flex-col justify-between space-y-4">
            <div className="space-y-3">
              <span className="font-mono-tech text-[11px] uppercase tracking-[0.14em] text-[#878179] font-semibold">
                Unsigned Indie Build Note
              </span>
              <h3 className="text-xl font-bold text-[#191817]">
                About the Windows SmartScreen prompt
              </h3>
              <p className="text-sm text-[#595550] leading-relaxed">
                SYNC SOUND is free, open-source software distributed without a paid
                commercial Authenticode certificate. Windows SmartScreen displays
                a notice for newly published unsigned executables.
              </p>
              <p className="text-sm text-[#595550] leading-relaxed">
                You can verify the SHA-256 checksum above against our public
                GitHub Actions build and inspect every line of source code before running.
              </p>
            </div>

            <a
              href={RELEASE.githubUrl}
              target="_blank"
              rel="noreferrer"
              className="inline-flex items-center justify-center px-5 py-3 rounded-full text-xs font-semibold border border-[#DFD8CA] bg-[#FCFBF8] hover:border-[#C65D32]/50 text-[#191817] transition-colors"
            >
              Inspect source code on GitHub
            </a>
          </div>
        </section>

        {/* FINAL CONVERSION CTA — Dark Signature Surface */}
        <section className="ink-card rounded-[28px] py-14 px-6 text-center text-[#F4EFE6]">
          <SyncSoundLogo className="w-14 h-14 rounded-2xl mx-auto mb-5 shadow-xl" />
          <h2 className="text-3xl sm:text-5xl font-extrabold tracking-tight">
            Your next movie night starts here.
          </h2>
          <p className="mt-3 text-[#B8B1A6] max-w-md mx-auto text-sm sm:text-base">
            Connect your headphones. Bring everyone in.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-3.5">
            <a
              href={RELEASE.exe.url}
              className="px-8 py-4 rounded-full text-sm font-semibold text-[#FCFBF8] bg-[#C65D32] hover:bg-[#D56A3E] shadow-[0_12px_30px_-6px_rgba(198,93,50,0.45)] transition-transform hover:scale-[1.02]"
            >
              Download SYNC SOUND {RELEASE.tag}
            </a>
            <a
              href={RELEASE.githubUrl}
              target="_blank"
              rel="noreferrer"
              className="px-7 py-4 rounded-full text-sm font-semibold border border-[#F4EFE6]/20 hover:border-[#F4EFE6]/45 text-[#F4EFE6] transition-colors"
            >
              Explore source on GitHub
            </a>
          </div>
        </section>
      </main>

      <footer className="relative z-10 border-t border-[#DFD8CA] py-8 px-6 text-xs text-[#878179]">
        <div className="max-w-6xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-4">
          <span>SYNC SOUND · {RELEASE.architecture} · {RELEASE.tag}</span>
          <div className="flex items-center gap-6">
            <a href={RELEASE.exe.url} className="hover:text-[#191817]">
              Download .exe
            </a>
            <a href={RELEASE.zip.url} className="hover:text-[#191817]">
              Portable .zip
            </a>
            <a
              href={RELEASE.githubUrl}
              target="_blank"
              rel="noreferrer"
              className="hover:text-[#191817]"
            >
              GitHub
            </a>
          </div>
        </div>
      </footer>
    </div>
  );
}
