import json
import re
import subprocess

from bs4 import BeautifulSoup


def fetch_yt_page(url: str) -> str:
    proc = subprocess.run(['curl', '-L', url], capture_output=True, check=True)
    page = proc.stdout.decode('utf-8')
    return page


def find_live_url(page:str) -> str | bool:
    m = re.search(r'(https\:\/\/www\.youtube.com\/watch\?v=.*?)\">', page)
    if m is None:
        return False
    return m.group(1)


def get_live_page_info(page:str) -> tuple:
    soup = BeautifulSoup(page, 'html.parser')

    for s in soup.find_all('script'):
        if 'ytInitialPlayerResponse' in s.text:
            payload = json.loads(s.text.strip().split('var ytInitialPlayerResponse = ') \
                                 [1].replace(';', ''))
            break

    title = payload['videoDetails']['title']
    channel_name = payload['videoDetails']['author']
    video_id = payload['videoDetails']['videoId']

    # checking if the channel is currently live
    try:
        payload['videoDetails']['isUpcoming']
    except KeyError:
        return (True, title, video_id, channel_name, None)

    startDate = payload['playabilityStatus']['liveStreamability']['liveStreamabilityRenderer'] \
        ['offlineSlate']['liveStreamOfflineSlateRenderer']['scheduledStartTime']

    return (False, title, video_id, channel_name, int(startDate))
