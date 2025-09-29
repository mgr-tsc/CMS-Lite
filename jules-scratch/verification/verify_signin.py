from playwright.sync_api import sync_playwright, Page, expect

def verify_signin_page(page: Page):
    """
    This script verifies that the new sign-in page is rendered correctly and captures console logs for debugging.
    """
    # Listen for all console events and print them
    page.on("console", lambda msg: print(f"BROWSER CONSOLE: {msg.text}"))

    # 1. Navigate to the sign-in page.
    page.goto("http://localhost:5174/login", wait_until="networkidle")

    # 2. Print page content for debugging
    print("PAGE CONTENT:")
    print(page.content())

    # 3. Wait for the "Sign In" heading to be visible to ensure the page has loaded.
    heading_locator = page.get_by_role("heading", name="Sign In")
    expect(heading_locator).to_be_visible(timeout=15000)

    # 4. Take a screenshot of the page.
    page.screenshot(path="jules-scratch/verification/verification.png")

if __name__ == "__main__":
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        try:
            verify_signin_page(page)
        finally:
            browser.close()