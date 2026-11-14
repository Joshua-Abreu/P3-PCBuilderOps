export function Logo() {
  return (
    <svg viewBox="0 0 32 32" className="size-8" aria-hidden="true">
      <rect x="3" y="3" width="26" height="26" rx="6" className="fill-teal-700" />
      <rect
        x="10"
        y="10"
        width="12"
        height="12"
        rx="2"
        fill="none"
        stroke="white"
        strokeWidth="2"
      />
      <path
        d="M13 6v4M19 6v4M13 22v4M19 22v4M6 13h4M6 19h4M22 13h4M22 19h4"
        stroke="white"
        strokeWidth="2"
        strokeLinecap="round"
      />
    </svg>
  )
}
