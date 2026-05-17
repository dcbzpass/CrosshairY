"""
encode_webhook.py! replace the discord webhook url in SurveyWindow.xaml.cs

usage: python encode_webhook.py

paste the output block into SurveyWindow.xaml.cs replacing the existing _whEnc array.
the xor key (_whKey) never changes, so only _whEnc needs updating.
"""

KEY = b"crosshairy_xk91"

def encode(url: str) -> list[int]:
    raw = url.encode("ascii")
    return [raw[i] ^ KEY[i % len(KEY)] for i in range(len(raw))]

def decode(encoded: list[int]) -> str:
    return bytes([encoded[i] ^ KEY[i % len(KEY)] for i in range(len(encoded))]).decode("ascii")

def format_cs_array(encoded: list[int]) -> str:
    rows = []
    for i in range(0, len(encoded), 16):
        chunk = encoded[i:i+16]
        rows.append("        " + ", ".join(f"0x{b:02x}" for b in chunk))
    return "    private static readonly byte[] _whEnc =\n    {\n" + ",\n".join(rows) + "\n    };"

def main():
    print("discord webhook url encoder for CrosshairY")
    print("paste the output block into SurveyWindow.xaml.cs, replacing the _whEnc array.\n")

    url = input("enter webhook url: ").strip()
    if not url.startswith("https://discord.com/api/webhooks/"):
        print("warning: url does not look like a discord webhook url, continuing anyway.")

    encoded = encode(url)

    # verify round-trip before printing
    decoded = decode(encoded)
    if decoded != url:
        print("error: round-trip check failed. url was not encoded correctly.")
        return

    print("\n--- copy this block into SurveyWindow.xaml.cs ---\n")
    print(format_cs_array(encoded))
    print("\n--- end of block ---")

if __name__ == "__main__":
    main()