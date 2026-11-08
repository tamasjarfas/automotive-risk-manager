// components/auth/AccessDenied.tsx
// Megjelenik, ha a felhasználó olyan oldalt nyit meg, amelyhez nincs jogosultsága.
// (A szerver ettől függetlenül is elutasítja a kéréseket 403-mal.)

interface Props {
  onBack: () => void
}

export default function AccessDenied({ onBack }: Props) {
  return (
    <div className="access-denied" role="alert">
      <div className="access-denied-icon">⛔</div>
      <h2>Nincs jogosultságod az oldal megtekintéséhez</h2>
      <p>
        Ehhez a funkcióhoz magasabb szintű szerepkör szükséges.
        Ha úgy gondolod, hogy erre szükséged van, fordulj a rendszergazdához.
      </p>
      <button className="btn btn-primary" onClick={onBack}>
        Vissza a kezdőlapra
      </button>
    </div>
  )
}

