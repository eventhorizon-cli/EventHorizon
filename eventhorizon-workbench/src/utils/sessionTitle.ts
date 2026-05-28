export function buildTemporarySessionTitle(input?: string) {
  const normalized = (input ?? "")
    .replace(/\s+/g, " ")
    .trim();

  if (!normalized) {
    return "New conversation";
  }

  return normalized.length <= 60 ? normalized : `${normalized.slice(0, 60).trimEnd()}…`;
}

