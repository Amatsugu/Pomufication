import json
import re
import shutil
import subprocess
import sys
import time
from datetime import datetime

from apscheduler.schedulers.background import BackgroundScheduler
import browser

IS_DEBUG = True

INVALID_CHARS = ['/', '\\', '\0', '`', '*', '|', ':', ':', '&']
REGEX_CONVERSIONS = {0: 2, 1: 8, 4: 16, 5: 64, 8: 512}



class Downloader:
    def __init__(self) -> None:
        self.active_downloads = {}
        self.active_ids = set()
        self.cfg = load_config()

    def do_checks(self):
        self.check_channels()
        self.clean_processes()


    def check_channels(self):
        for channel in self.cfg['Channels']:
            if channel['Enabled'] is False:
                continue
            channel_id = channel["ChannelId"]
            filters = channel['FilterKeywords']
            self.check_single_channel(channel_id, filters)


    def check_single_channel(self, ch_id:str, filters: list) -> None:
        link = f"https://www.youtube.com/channel/{ch_id}/live"
        live_page = browser.fetch_yt_page(link)

        live_link = browser.find_live_url(live_page)
        if live_link is False:
            return
        live_page = browser.fetch_yt_page(live_link)
        is_live, title, video_id, ch_name, start_date = browser.get_live_page_info(live_page)

        if not self.filter_stream(title, filters):
            logger.info("Skipping %s - %s since not in filter.", ch_name, title)
            return

        if video_id in self.active_ids:
            print(f"Already downloading {ch_name}")
            return

        if is_live:
            print(f"Found a currently live stream! {ch_name} - {title}")
        else:
            print(f"Found a scheduled live stream! {ch_name} - {title} \
                  -> {datetime.fromtimestamp(start_date)}")

        self.streamlink_helper(live_link, ch_name, video_id)


    def filter_stream(self, title, filters):
        for _f in filters:
            if not _f['Enabled']:
                continue
            res = self.filter_stream_helper(_f, title)
            if res is True:
                return True
        return False

    def filter_stream_helper(self, _filter, title):
        _type = _filter['Type']

        if _type == 0:
            return match_words(title, _filter['Filters'], _filter["Comparison"])

        return match_regex(title, _filter['Filters'], _filter["RegexOptions"])


    def streamlink_helper(self, link, channel, video_id):
        print(f"Spawning streamlink child process for {channel}")
        _process = self.start_streamlink(link, channel)
        self.active_downloads[video_id] = _process
        self.active_ids.add(video_id)


    def clean_processes(self):
        items = [f for f in  self.active_ids]
        for _id in items:
            _process = self.active_downloads[_id]
            if _process.poll() is not None:
                print(f"Process {_id} has finished")

                del self.active_downloads[_id]
                self.active_ids.remove(_id)


    def start_streamlink(self, url: str, name: str) -> subprocess.Popen:
        fn = sanitize(f"\"{name}.mp4\"")
        streamlink = shutil.which("streamlink")
        return subprocess.Popen([streamlink, url, "best", "--stream-segment-timeout",
                                "60", "--stream-timeout", "360","--retry-streams",
                                "30", "-o", fn], stdout=subprocess.DEVNULL,
                                stderr=subprocess.STDOUT)


def match_words(name: str, words: list, comparison: int) -> bool:
    if comparison % 2 == 1:
        flag_options = re.IGNORECASE
    else:
        flag_options = re.NOFLAG
    for pat in words:
        _ret = re.findall(pat, name, flags=flag_options)
        if len(_ret) > 0:
            return True
    return False

def match_regex(name: str, regex: list, options: int) -> bool:
    flag_options = get_flags(options)
    for pat in regex:
        _ret = re.search(pat, name, flags=flag_options)
        if _ret is not None:
            return True
    return False

def get_flags(options: int) -> int:
    new_flag = 0
    bits = f"{options:09b}"[::-1]
    keys = REGEX_CONVERSIONS.keys()
    for idx, bit in enumerate(bits):
        if bit == '1' and idx in keys:
            new_flag += REGEX_CONVERSIONS[idx]

    return new_flag

def sanitize(value: str) -> str:
    for sub in INVALID_CHARS:
        value = value.replace(sub, '')
    m = re.match(r"^-+(.*)", value)
    if m:
        return m.group(1)
    return value


def load_config() -> dict :
    path = "config.json"
    with open(path, "r", encoding="utf-8") as cfg:
        data = json.load(cfg)
        return data


if __name__ == '__main__':
    app = Downloader()
    scheduler = BackgroundScheduler()
    scheduler.add_job(app.do_checks, 'interval', minutes=5)
    scheduler.start()
    try:
        app.do_checks()
        while True:
            time.sleep(5)

    except( KeyboardInterrupt, SystemExit):
        for process in app.active_downloads.values():
            process.kill()
        scheduler.shutdown()
        sys.exit(0)
    finally:
        for process in app.active_downloads.values():
            process.kill()
