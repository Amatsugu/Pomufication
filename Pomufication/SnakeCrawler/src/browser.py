import json
import re
import subprocess

from bs4 import BeautifulSoup

def fetch_yt_page(url: str) -> str:
    proc = subprocess.run(['curl', '-L', url], capture_output=True, check=True)
    page = proc.stdout.decode('utf-8')
    return page

def fetch_channel_page(url: str) -> str:
    proc = subprocess.run(['curl', '-L', '-H', 'Accept-Language: en-US,en;q=0.5', url], capture_output=True, check=True)
    page = proc.stdout.decode('utf-8')
    return page

def get_channel_info(page:str) -> tuple:
    soup = BeautifulSoup(page, 'html.parser')
    for e in soup.find_all('script'):
        if e.text.startswith('var ytInitialData'):
            payload = json.loads(e.text.strip().split('var ytInitialData = ') \
                                 [1].replace(';', ''))
            break


    ch_name = payload['metadata']['channelMetadataRenderer']['title']

    # check for upcoming livestreams tab

    h_tabs_main = payload['contents']['twoColumnBrowseResultsRenderer']['tabs'][0]

    aux = h_tabs_main['tabRenderer']['content']['sectionListRenderer']['contents']

    upcoming_livestreams = None
    for section in aux:
        try:
            sub_section = section['itemSectionRenderer']['contents'][0]['shelfRenderer']
            title = sub_section['title']['runs'][0]['text']
            if title == 'Upcoming live streams':
                up_streams_aux = sub_section['content'].keys()

                if 'horizontalListRenderer' in up_streams_aux:
                    upcoming_livestreams = sub_section['content']['horizontalListRenderer']['items']
                    break
                elif 'expandedShelfContentsRenderer' in up_streams_aux:
                    upcoming_livestreams = sub_section['content']['expandedShelfContentsRenderer']['items']
                    break
                else:
                    pass
        except KeyError:
            pass
    
    if upcoming_livestreams is None:
        return (ch_name, None)


    upcoming: list = []
    if len(upcoming_livestreams) == 1:
        try:
            video_id = upcoming_livestreams[0]['videoRenderer']['videoId']
            video_title = upcoming_livestreams[0]['videoRenderer']['title']['simpleText']
            start_date_ts = int(upcoming_livestreams[0]['videoRenderer']['upcomingEventData']['startTime'])
        except KeyError:
            pass


    for stream in upcoming_livestreams:
        try:
            video_id = stream['gridVideoRenderer']['videoId']
            video_title = stream['gridVideoRenderer']['title']['simpleText']
            start_date_ts = int(stream['gridVideoRenderer']['upcomingEventData']['startTime'])
        except KeyError:
            pass
        
        upcoming.append((video_title, video_id, start_date_ts))

    return (ch_name, upcoming)


def find_live_url(page:str) -> str | bool:
    soup = BeautifulSoup(page, 'html.parser')
    for e in soup.find_all('link', rel=True, ):
        if e['rel'][0] == 'canonical':
            url = e['href']
    
    if re.match(r'https\:\/\/www\.youtube.com\/watch\?v=', url):
        return url
    return False


def get_live_page_info(page:str) -> tuple:
    soup = BeautifulSoup(page, 'html.parser')

    for s in soup.find_all('script'):
        if 'ytInitialPlayerResponse' in s.text:
            payload = json.loads(s.text.strip().split('var ytInitialPlayerResponse = ') \
                                 [1].replace(';', ''))
            break

    title = payload['videoDetails']['title']
    video_id = payload['videoDetails']['videoId']
    ch_name = payload['videoDetails']['author']

    # checking if the channel is currently live
    try:
        payload['videoDetails']['isUpcoming']
    except KeyError:
        return (True, title, video_id, ch_name, -1)

    startDate = payload['playabilityStatus']['liveStreamability']['liveStreamabilityRenderer'] \
        ['offlineSlate']['liveStreamOfflineSlateRenderer']['scheduledStartTime']

    return (False, title, video_id, ch_name, int(startDate))


if __name__ == "__main__":
    # a = fetch_channel_page('https://www.youtube.com/@OozoraSubaru')
    a = fetch_channel_page('https://www.youtube.com/@Ninomaeinanis')
    ch, b = get_channel_info(a)

    print(ch)
    if b is not None:
        for v in b:
            print(v)