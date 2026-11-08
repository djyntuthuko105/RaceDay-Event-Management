"""HTTP checks against a local demo stack only; no live Azure account is used."""
import base64
from datetime import date, timedelta
import html
import http.cookiejar
import json
import re
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid

API = "http://127.0.0.1:5080"
WEB = "http://127.0.0.1:5000"
PNG = base64.b64decode("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/L9sAAAAASUVORK5CYII=")


def browser():
    return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))


def request(client, url, data=None, content_type=None, method=None):
    headers = {"Content-Type": content_type} if content_type else {}
    return client.open(urllib.request.Request(url, data=data, headers=headers, method=method), timeout=25)


def wait_for(url):
    for attempt in range(90):
        try:
            with urllib.request.urlopen(url, timeout=3) as response:
                if response.status == 200:
                    return
        except (urllib.error.URLError, TimeoutError):
            pass
        time.sleep(1)
    raise RuntimeError("Local stack did not become ready: " + url)


def api_json(client, path, data=None, method=None):
    body = json.dumps(data).encode() if data is not None else None
    with request(client, API + path, body, "application/json" if body else None, method) as response:
        return json.load(response) if response.status != 204 else None


def token(client, path):
    with request(client, WEB + path) as response:
        page = response.read().decode()
    match = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page)
    assert match, "The form did not include an antiforgery token."
    return html.unescape(match.group(1))


def multipart(fields, file_field):
    boundary = "RaceDaySmoke" + uuid.uuid4().hex
    body = bytearray()
    for name, value in fields.items():
        body.extend(f'--{boundary}\r\nContent-Disposition: form-data; name="{name}"\r\n\r\n{value}\r\n'.encode())
    body.extend(f'--{boundary}\r\nContent-Disposition: form-data; name="{file_field}"; filename="smoke.png"\r\nContent-Type: image/png\r\n\r\n'.encode())
    body.extend(PNG)
    body.extend(f'\r\n--{boundary}--\r\n'.encode())
    return bytes(body), "multipart/form-data; boundary=" + boundary


def main():
    wait_for(API + "/health")
    wait_for(WEB + "/")
    api, web = browser(), browser()
    email = "smoke-" + uuid.uuid4().hex + "@example.test"
    account = {"firstName": "Local", "lastName": "Smoke Test", "email": email, "password": "LocalSmoke123!", "role": "Organiser"}
    api_json(api, "/api/auth/register", account)
    api_json(api, "/api/auth/login", {"email": email, "password": account["password"]})
    login = urllib.parse.urlencode({"Email": email, "Password": account["password"], "__RequestVerificationToken": token(web, "/Account/Login")}).encode()
    with request(web, WEB + "/Account/Login", login, "application/x-www-form-urlencoded") as response:
        assert "/Organiser" in response.url, "MVC login failed."
    fields = {
        "EventName": "Local smoke test race", "Description": "Temporary local integration fixture.",
        "EventDate": (date.today() + timedelta(days=30)).isoformat(), "RegistrationDeadline": (date.today() + timedelta(days=28)).isoformat(), "DistanceKm": "10",
        "LocationId": str(api_json(api, "/api/locations")[0]["locationId"]),
        "EventTypeId": str(api_json(api, "/api/event-types")[0]["eventTypeId"]),
        "__RequestVerificationToken": token(web, "/Organiser/Create"),
    }
    body, content_type = multipart(fields, "BannerFile")
    with request(web, WEB + "/Organiser/Create", body, content_type) as response:
        assert "/Organiser/Categories/" in response.url, "MVC event creation failed."
        event_id = int(urllib.parse.urlparse(response.url).path.rsplit("/", 1)[1])
    images = api_json(api, f"/api/events/{event_id}/images")
    assert len(images) == 1, "The API did not persist the banner."
    image = images[0]
    assert "/raceday-images/events/" in image["imageUrl"], "This check requires Blob storage, not Local files."
    with request(web, WEB + f"/Media/Event/{image['eventImageId']}") as response:
        assert response.read() == PNG, "The Blob round-trip changed the banner bytes."
    with request(web, WEB + f"/Events/Details/{event_id}") as response:
        assert "/Media/Event/" in response.read().decode(), "The event page did not display its banner."
    fields = {"FirstName": "Local", "LastName": "Smoke Test", "PhoneNumber": "", "__RequestVerificationToken": token(web, "/Profile")}
    body, content_type = multipart(fields, "PictureFile")
    with request(web, WEB + "/Profile", body, content_type) as response:
        assert urllib.parse.urlparse(response.url).path == "/Profile", "Profile update failed."
    assert "/raceday-images/profiles/" in api_json(api, "/api/users/me")["profilePictureUrl"]
    with request(web, WEB + "/Profile/Picture") as response:
        assert response.read() == PNG, "The private profile image did not round-trip."
    logout = urllib.parse.urlencode({"__RequestVerificationToken": token(web, "/Profile")}).encode()
    with request(web, WEB + "/Account/Logout", logout, "application/x-www-form-urlencoded") as response:
        assert "/Account/Login" in response.url
    with request(web, WEB + "/Profile") as response:
        assert "/Account/Login" in response.url, "Logout did not clear the MVC session."
    api_json(api, f"/api/event-images/{image['eventImageId']}", method="DELETE")
    api_json(api, f"/api/events/{event_id}", method="DELETE")
    print("PASS: MVC login, event creation, API-mediated Blob banner, persisted URL, image display, profile upload and logout.")


if __name__ == "__main__":
    main()
