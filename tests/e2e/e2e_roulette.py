"""E2E check of the Roulette plugin in a real Jellyfin web client (headless Chromium).

Usage: python tests/e2e/e2e_roulette.py [base_url] [user] [password] [playlist name]
Logs in, opens the Playlists view, clicks the first playlist card, presses
"Surprise Me", waits for the spin, presses "Play it!" and reports console
messages, dialogs and the final URL / page state. Screenshots go to ./e2e-shots.
"""
import os
import sys

from playwright.sync_api import sync_playwright

BASE = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:8096"
USER = sys.argv[2] if len(sys.argv) > 2 else "admin"
PASS = sys.argv[3] if len(sys.argv) > 3 else "admin"
PLAYLIST = sys.argv[4] if len(sys.argv) > 4 else None
SHOTS = os.path.join(os.getcwd(), "e2e-shots")
os.makedirs(SHOTS, exist_ok=True)


def shot(page, name):
    page.screenshot(path=os.path.join(SHOTS, f"{name}.png"))


with sync_playwright() as p:
    browser = p.chromium.launch()
    page = browser.new_page(viewport={"width": 1280, "height": 800})
    page.on("console", lambda m: print(f"[console.{m.type}] {m.text}"))
    page.on("pageerror", lambda e: print(f"[pageerror] {e}"))

    def on_dialog(d):
        print(f"[dialog.{d.type}] {d.message!r}")
        d.accept()

    page.on("dialog", on_dialog)
    page.on("response", lambda r: r.status >= 400 and print(f"[http {r.status}] {r.url}"))

    page.goto(f"{BASE}/web/")
    page.wait_for_selector("#txtManualName", timeout=60000)
    page.fill("#txtManualName", USER)
    page.fill("#txtManualPassword", PASS)
    page.click("button[type=submit]")
    page.wait_for_function("window.ApiClient && window.ApiClient.getCurrentUserId()", timeout=60000)
    page.wait_for_timeout(3000)

    # Find the Playlists user view and open it the way a user would.
    views = page.evaluate(
        """async () => {
            const c = window.ApiClient;
            const r = await c.getJSON(c.getUrl(`Users/${c.getCurrentUserId()}/Views`));
            return r.Items.map(i => ({Id: i.Id, Name: i.Name, CollectionType: i.CollectionType}));
        }"""
    )
    print("[views]", views)
    pl_view = next(v for v in views if v.get("CollectionType") == "playlists")
    server_id = page.evaluate("window.ApiClient.serverId()")
    page.goto(f"{BASE}/web/#/list?parentId={pl_view['Id']}&serverId={server_id}")
    page.wait_for_selector(".card[data-type=Playlist]", timeout=30000)
    page.wait_for_timeout(1500)
    shot(page, "1-playlists")

    card = page.locator(".card[data-type=Playlist]").filter(has_text=PLAYLIST).first if PLAYLIST else page.locator(".card[data-type=Playlist]").first
    print("[card]", card.get_attribute("data-id"), card.inner_text().replace("\n", " | "))
    box = card.locator(".cardImageContainer").first.bounding_box()
    page.mouse.move(box["x"] + box["width"] / 2, box["y"] + 15)
    page.mouse.click(box["x"] + box["width"] / 2, box["y"] + 15)
    page.wait_for_timeout(2500)
    shot(page, "2-after-click")
    print("[url after card click]", page.url)

    if page.locator("#rouletteSurpriseBtn").count() == 0:
        print("[result] Roulette modal did NOT appear")
    else:
        page.click("#rouletteSurpriseBtn")
        page.wait_for_timeout(6000)
        shot(page, "3-after-spin")
        btns = page.locator("button[id^=roulette]")
        print("[buttons]", [btns.nth(i).get_attribute("id") + "=" + btns.nth(i).inner_text() for i in range(btns.count())])
        watch = page.locator("#rouletteWatchBtn, button:has-text('Play it')").first
        if watch.count():
            watch.click()
            try:
                page.wait_for_selector(".itemName, .detailPagePrimaryContainer", timeout=30000)
            except Exception:
                print("[result] details page did not render within 30s")
            page.wait_for_timeout(1500)
            print("[url after Play it]", page.url)
            shot(page, "4-after-play")
            print("[page text]", page.locator("body").inner_text()[:600].replace("\n", " | "))
    browser.close()
