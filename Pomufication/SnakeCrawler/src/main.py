import json
import sys
import subprocess
import shutil
import re
import time
from bs4 import BeautifulSoup

IS_DEBUG = sys.platform == "win32"

INVALID_CHARS = ['/', '\\', '\0', '`', '*', '|', ':', ':', '&']
REGEX_CONVERSIONS = {0: 2, 1: 8, 4: 16, 5: 64, 8: 512}

def main():
	while True:
		do_checks()
		time.sleep(5 * 60)

def do_checks():
	cfg = load_config()
	
	active_processes = {}

	check_channels(cfg["Channels"], active_processes)
	clean_processes(active_processes)


def clean_processes(active_processes : dict):
	items = list(active_processes.items())
	for id, process in items:
		if not process.poll():
			del active_processes[id]
	pass

def check_channels(channels : list, active_processes: dict):
	for channel in channels:
		check_channel(channel, active_processes)
		break

def check_channel(channel: dict, active_processes: dict):
	if not channel["Enabled"]:
		return

	id = "UCSUu1lih2RifWkKtDOJdsBA" if IS_DEBUG else channel["ChannelId"]
	if id in active_processes.keys():
		return

	url = f"https://youtube.com/channel/{id}/live"
	process = subprocess.run(["curl", "-L", url], capture_output=True)
	html = process.stdout.decode("utf-8")

	(streamUrl, name) = read_channel_html(html)
	if not should_download(name, channel["FilterKeywords"]):
		return

	pid = start_streamlink(streamUrl, name)
	active_processes[id] = pid

def should_download(name: str, keywords: list) -> bool:
	if IS_DEBUG:
		return True
	for	keyword in keywords:
		if not match_keyword(name, keyword):
			return False
	return True

def match_keyword(name: str, keyword: dict):
	if not keyword["Enabled"]:
		return True
	if keyword["Type"] == 1:
		return match_words(name, keyword["Filters"], keyword["Comparison"])
	else:
		return match_regex(name, keyword["Filters"], keyword["RegexOptions"])

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
	for idx, bit in enumerate(bits):
		if bit == '1' and idx in REGEX_CONVERSIONS.keys:
			new_flag += REGEX_CONVERSIONS[idx]

	return new_flag

def start_streamlink(url: str, name: str) -> subprocess.Popen:
	fn = sanitize(f"\"{name}.mp4\"")
	streamlink = shutil.which("streamlink")
	return subprocess.Popen([streamlink, url, "best", "--stream-segment-timeout", "60", "--stream-timeout", "360","--retry-streams", "30", "-o", fn])

def sanitize(value: str) -> str:
	for sub in INVALID_CHARS:
		value = value.replace(sub, '')
	m = re.match(r"^-+(.*)", value)
	if m:
		return m.group(1)
	return value


def read_channel_html(html: str | bytes) -> tuple:
	soup = BeautifulSoup(html, 'html.parser')
	contentInfo = soup.find(rel="canonical")
	name = soup.find("title").contents[0].text.replace(" - YouTube", "")
	return contentInfo.attrs["href"], name

def load_config() -> dict :
	path = "config.json"
	with open(path, "r") as cfg:
		data = json.load(cfg)
		return data


if __name__ == '__main__':
	main()