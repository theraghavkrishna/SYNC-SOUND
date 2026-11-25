import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "SYNC SOUND — One sound. Everyone listening.",
  description:
    "Connect multiple Bluetooth headphones and speakers to your Windows 11 laptop and listen together in one click.",
  openGraph: {
    title: "SYNC SOUND — One sound. Everyone listening.",
    description:
      "Share any Windows audio across multiple headphones simultaneously with a calm native app and instant Ctrl+Shift+S Quick Sidebar.",
    type: "website",
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
