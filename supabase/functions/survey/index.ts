import { serve } from "https://deno.land/std@0.224.0/http/server.ts";

const WEBHOOK = Deno.env.get("DISCORD_WEBHOOK_URL") ?? "";

function clamp(value: unknown, max: number): string {
  const s = typeof value === "string" ? value : String(value ?? "");
  return s.length > max ? s.slice(0, max) : s;
}

serve(async (req) => {
  if (req.method !== "POST") {
    return new Response("method not allowed", { status: 405 });
  }

  if (!WEBHOOK) {
    return new Response("not configured", { status: 500 });
  }

  let body: Record<string, unknown>;
  try {
    body = await req.json();
  } catch {
    return new Response("bad request", { status: 400 });
  }

  const question = clamp(body.question, 256);
  const answer = clamp(body.answer, 1024);
  const launch = clamp(body.launch, 32);

  if (!question || !answer) {
    return new Response("bad request", { status: 400 });
  }

  const payload = {
    embeds: [
      {
        title: "CrosshairY Survey",
        color: 2302755,
        fields: [
          { name: "question", value: question, inline: false },
          { name: "answer", value: answer, inline: false },
          { name: "launch", value: launch, inline: false },
        ],
      },
    ],
  };

  try {
    const res = await fetch(WEBHOOK, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    });
    if (!res.ok) {
      return new Response("upstream error", { status: 502 });
    }
  } catch {
    return new Response("upstream error", { status: 502 });
  }

  return new Response("ok", { status: 200 });
});
