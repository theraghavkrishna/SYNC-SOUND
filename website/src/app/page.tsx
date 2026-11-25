import InteractiveShowcase from "@/components/InteractiveShowcase";

const GITHUB_REPO_URL = "https://github.com/theraghavkrishna/SYNC-SOUND";
const DOWNLOAD_URL = "https://github.com/theraghavkrishna/SYNC-SOUND/releases/download/v1.0.0/SYNC-SOUND-x64.exe";
const PORTABLE_ZIP_URL = "https://github.com/theraghavkrishna/SYNC-SOUND/releases/download/v1.0.0/SYNC-SOUND-Portable-x64.zip";

export default function HomePage() {
  return (
    <>
      <header className="nav">
        <div className="nav-inner">
          <a href="#" className="brand">
            <span className="wave-mark" aria-hidden="true">
              <span className="wave-bar" style={{ height: 10 }} />
              <span className="wave-bar" style={{ height: 18 }} />
              <span className="wave-bar dark" style={{ height: 13 }} />
            </span>
            <span>SYNC SOUND</span>
          </a>

          <nav className="nav-links" aria-label="Primary">
            <a href="#how-it-works">How it works</a>
            <a href="#sidebar">Quick Sidebar</a>
            <a href="#engineering">Built for real life</a>
            <a href={GITHUB_REPO_URL} target="_blank" rel="noreferrer">
              GitHub
            </a>
          </nav>

          <a href={DOWNLOAD_URL} target="_blank" rel="noreferrer" className="btn-primary sm">
            Download for Windows
          </a>
        </div>
      </header>

      <main>
        {/* Hero */}
        <section className="hero">
          <div className="container">
            <div className="eyebrow">
              <span className="eyebrow-dot" />
              Windows 11 · Free &amp; Open Source · v1.0
            </div>

            <h1>One sound. Everyone listening.</h1>

            <p className="hero-sub">
              Your laptop has one headphone jack and more than one person on the couch. Connect
              every pair of Bluetooth earbuds you have, press one button, and hear movies, music,
              and games together.
            </p>

            <div className="hero-actions">
              <a href={DOWNLOAD_URL} target="_blank" rel="noreferrer" className="btn-primary">
                Download for Windows
              </a>
              <a href={GITHUB_REPO_URL} target="_blank" rel="noreferrer" className="btn-secondary">
                View on GitHub
              </a>
            </div>

            <div className="hero-meta">
              Windows 11 (x64) · Self-contained portable &amp; installer · No account, no cloud
            </div>

            <InteractiveShowcase />
          </div>
        </section>

        {/* Three Steps */}
        <section id="how-it-works" className="section">
          <div className="container">
            <div className="section-header">
              <h2>Connect. Select everyone. Play.</h2>
              <p>
                No virtual cables, no audio-mixer clutter, and no 20-minute driver setup. Just your
                headphones and whatever you are watching.
              </p>
            </div>

            <div className="grid-3">
              <article className="card">
                <div className="step-num">01 · PAIR IN WINDOWS</div>
                <h3>Connect your headphones</h3>
                <p>
                  Pair AirPods, OnePlus Buds, Galaxy Buds, or plug in wired headphones just like you
                  always do. SYNC SOUND spots them immediately.
                </p>
              </article>

              <article className="card">
                <div className="step-num">02 · PICK EVERYONE</div>
                <h3>Select who is listening</h3>
                <p>
                  Toggle any mix of headphones and speakers with one tap. SYNC SOUND remembers your
                  favorites so next movie night starts even faster.
                </p>
              </article>

              <article className="card">
                <div className="step-num">03 · PRESS START</div>
                <h3>Play anything on Windows</h3>
                <p>
                  Open YouTube, Netflix, Spotify, or drop a song into the built-in Music Player.
                  Everyone hears the same moment together.
                </p>
              </article>
            </div>
          </div>
        </section>

        {/* Quick Sidebar + Built-in Player */}
        <section id="sidebar" className="section" style={{ paddingTop: 12 }}>
          <div className="container">
            <div className="grid-2">
              <article className="card" style={{ padding: 38 }}>
                <div className="step-num">QUICK SIDEBAR · CTRL + SHIFT + S</div>
                <h3 style={{ fontSize: 26, marginBottom: 12 }}>Never leave the movie.</h3>
                <p style={{ marginBottom: 20 }}>
                  Press <span className="kbd">Ctrl</span> + <span className="kbd">Shift</span> +{" "}
                  <span className="kbd">S</span> from inside Netflix, VLC, or your browser. A calm
                  floating sidebar glides in from the edge of your screen so you can add a friend’s
                  earbuds or stop sharing—then tuck it away with <span className="kbd">Esc</span>.
                </p>
                <p style={{ fontSize: 13.5, color: "var(--ink-muted)" }}>
                  Lives quietly in your Windows system tray when closed, ready whenever someone
                  walks in with headphones.
                </p>
              </article>

              <article className="card" style={{ padding: 38 }}>
                <div className="step-num">BUILT-IN MUSIC PLAYER</div>
                <h3 style={{ fontSize: 26, marginBottom: 12 }}>Drop a song. Test the room.</h3>
                <p style={{ marginBottom: 20 }}>
                  Drag and drop MP3, WAV, FLAC, or M4A tracks straight into SYNC SOUND. Album artwork,
                  smooth seeking, queue management, and instant multi-headphone playback—built right
                  in with zero extra setup.
                </p>
                <p style={{ fontSize: 13.5, color: "var(--ink-muted)" }}>
                  Perfect for checking headphone balance or listening to an album together on a
                  flight or train.
                </p>
              </article>
            </div>
          </div>
        </section>

        {/* Engineering Honesty */}
        <section id="engineering" className="section" style={{ paddingTop: 12 }}>
          <div className="container">
            <div className="section-header">
              <h2>Complexity hidden. Reliability visible.</h2>
              <p>
                Native C# and Windows WASAPI engineering underneath, wrapped in software that feels
                effortless on the surface.
              </p>
            </div>

            <div className="grid-3">
              <article className="card">
                <h3>Fault-isolated audio</h3>
                <p>
                  Every pair of headphones gets its own independent stream. If one friend’s earbuds
                  run out of battery, everyone else keeps listening without a crash.
                </p>
              </article>

              <article className="card">
                <h3>Zero feedback loops</h3>
                <p>
                  Your primary Windows output keeps playing normally while SYNC SOUND cleanly fans
                  out audio to your additional headphones—never doubling back on itself.
                </p>
              </article>

              <article className="card">
                <h3>100% local &amp; private</h3>
                <p>
                  No sign-in, no cloud servers, and zero telemetry. Your audio never leaves your
                  laptop, and your preferences stay in a simple local file.
                </p>
              </article>
            </div>
          </div>
        </section>

        {/* Download CTA */}
        <section id="download" className="section" style={{ paddingTop: 12 }}>
          <div className="container">
            <div className="download-box">
              <h2>Ready to share the sound?</h2>
              <p>
                Download SYNC SOUND for Windows 11, connect two pairs of earbuds, and press Start
                sharing.
              </p>

              <div className="hero-actions">
                <a href={DOWNLOAD_URL} className="btn-primary">
                  Download SYNC-SOUND-x64.exe
                </a>
                <a href={PORTABLE_ZIP_URL} className="btn-secondary">
                  Portable ZIP (x64)
                </a>
                <a href={GITHUB_REPO_URL} target="_blank" rel="noreferrer" className="btn-secondary">
                  Browse Source on GitHub
                </a>
              </div>

              <div className="smartscreen-note">
                <strong>First-launch tip:</strong> Because SYNC SOUND is an independent open-source
                release, Windows SmartScreen may ask once on launch—click <em>More info</em> →{" "}
                <em>Run anyway</em>.
              </div>
            </div>
          </div>
        </section>
      </main>

      <footer>
        <div className="container footer-inner">
          <div>
            <strong>SYNC SOUND</strong> — One sound. Everyone listening.
          </div>
          <div style={{ display: "flex", gap: 22 }}>
            <a href={GITHUB_REPO_URL} target="_blank" rel="noreferrer">
              GitHub · theraghavkrishna/SYNC-SOUND
            </a>
            <a href={`${GITHUB_REPO_URL}/releases`} target="_blank" rel="noreferrer">
              Releases
            </a>
          </div>
        </div>
      </footer>
    </>
  );
}
