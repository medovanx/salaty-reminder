# Changelog

All notable changes to Salaty Reminder are listed here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and versions follow [Semantic Versioning](https://semver.org/).

## [1.6.7] - 2026-11-22

### Added
- Qiyam al-Layl on the card, after Isha, at the start of the night's last third, with a starry moon icon. It is voluntary, so it never becomes the next prayer and has no reminder.

## [1.6.6] - 2026-11-22

### Fixed
- On Friday the Jumu'ah row showed Dhuhr's sunnah (4 before, 2 after). It now says 2 to 4 rak'ahs after, with none before, as in Islamweb fatwa 55016.

### Changed
- Duha now gives its rak'ahs: 2 to 8, from 15 minutes after sunrise.

## [1.6.5] - 2026-11-18

### Fixed
- Start with Windows works again on recent Windows 11 builds, which skip a startup entry unless it is also marked as enabled in Windows' startup list. Turning it on now writes that mark too.

## [1.6.4] - 2026-11-17

### Changed
- The night times and the day and night length line up in two columns: Midnight over the day length, the last third over the night length.

## [1.6.3] - 2026-11-17

### Added
- The card shows how long the day and the night are, under the night times. The day runs from Fajr to Maghrib and the night from Maghrib to the next Fajr, the same night that Midnight halves.

## [1.6.2] - 2026-11-17

### Changed
- Back to "Midnight", with an "i" beside it on the card: point at it for a note that this is the Islamic midnight, halfway from Maghrib to Fajr, not 12:00. The icon is centred exactly on the text.

## [1.6.1] - 2026-11-17

### Changed
- "Midnight" is now "Middle of the night": it is halfway from Maghrib to Fajr, not 12:00.

### Fixed
- The night times on the hover card were cut off. They now sit on their own rows, as do Imsak and Iftar in Ramadan.

## [1.6.0] - 2026-11-17

### Added
- Ramadan mode: in Ramadan the widget and the card count down to Iftar by day and to the end of Suhoor by night. The card's bar follows the fast from Fajr to Maghrib, Fajr shows Imsak (10 minutes before), Maghrib's notification says it is Iftar, and a Suhoor reminder comes 45 minutes before Fajr.
- Rest the mouse on the widget for a small card with today's times, the Hijri date and any occasion, plus Imsak and Iftar in Ramadan.
- Islamic occasions on the calendar and the card: the Islamic New Year, Tasu'a and Ashura, the start of Ramadan and its last ten nights, Eid al-Fitr, the first ten days of Dhu al-Hijjah, Arafah, Eid al-Adha and Tashreeq. Sunnah fasting days are named too (Monday, Thursday, the white days, Arafah, Ashura, six of Shawwal), never on an Eid or a day of Tashreeq.
- Friday reminders for Surat Al-Kahf and for Jumu'ah 45 minutes ahead, morning and evening adhkar reminders, and an optional reminder the evening before a sunnah fast.
- Midnight and the start of the last third of the night under the prayer list and on the hover card.
- A new Extras page in Settings turns each of these on or off.

## [1.5.0] - 2026-11-17

### Changed
- Prayer icons on the card are in full colour, following the sky: a blue dawn moon for Fajr, an orange rising sun, a yellow noon sun for Dhuhr, an amber afternoon sun for Asr, a red setting sun for Maghrib and a pale moon with a gold star for Isha. Prayers that have passed fade. The icons are drawn at four times their size and scaled down, so their edges stay smooth.

## [1.4.4] - 2026-11-17

### Changed
- The version line at the top of the widget's right-click menu shows the app icon.

### Fixed
- Menu text sat above the middle of its row, so the gaps around separators were uneven and the hover highlight was off-centre. Text, icons and highlight are now centred in each row.

## [1.4.3] - 2026-11-17

### Fixed
- Icons and check marks in the widget's right-click menu sat a little low beside their text. They now centre on the text line.

## [1.4.2] - 2026-11-17

### Changed
- The widget's right-click menu is restyled: rounded corners and rows, an icon beside each item, and the app version at the top. While an update runs, its progress shows in place of Check for updates.
- Switching the language from the menu closes an open Settings window, so it can't save the old language back.

## [1.4.1] - 2026-11-17

### Changed
- The widget's right-click menu follows the app's light or dark theme.
- While checking for or installing an update, the taskbar widget shows a progress bar along its bottom edge and the status on its second line, so a check from the right-click menu is visible too.

## [1.4.0] - 2026-11-17

### Added
- Right-click the taskbar widget to switch the language between English and Arabic.
- The Updates page shows progress: a bar takes the place of Check now while it checks, downloads (with percent and size) and verifies an update.
- GitHub page and Release notes buttons on the Updates page.
- The Search button for a city shows a spinner while it searches.

## [1.3.0] - 2026-11-16

### Changed
- No more icon in the notification area. Notifications are now Windows toasts that stay in the notification centre; clicking one opens the prayer card, and clicking an update toast installs the update.

## [1.2.1] - 2026-11-16

### Fixed
- Disabled buttons in Settings (Browse with the adhan off, Check now and Search while busy) showed black text on the dark theme. They now fade with the theme.

## [1.2.0] - 2026-11-16

### Added
- The Updates page in Settings links to the project on GitHub.

## [1.1.1] - 2026-11-16

### Changed
- README shows the calendar's month and year pickers.

## [1.1.0] - 2026-11-16

### Added
- Click the calendar's month title to pick a month, and click again to pick a year, instead of stepping one month at a time. Esc steps back to the days.

## [1.0.5] - 2026-11-16

### Changed
- The calculation method under the prayer list is centred.

## [1.0.4] - 2026-11-16

### Fixed
- Closing a dropdown in Settings could crash the app on the next click, removing the widget from the taskbar. If anything else ever escapes like that, the app now logs it and restarts itself once.

### Changed
- Asr shows no sunnah line at all; its name sits centred in the row.

## [1.0.3] - 2026-11-16

### Changed
- Asr shows "No confirmed sunnah" instead of 4 rak'ahs before, since those are not among the confirmed rawatib.

## [1.0.2] - 2026-11-16

### Fixed
- After a reboot the widget could sit on the left over the weather widget, because Windows reports the tray area at x=0. It now finds the real tray edge through UI Automation and keeps correcting its position.

### Changed
- Repository reorganised into src/ and tests/ with a Core library for the prayer-time maths, one type per file and folder namespaces.
- Unit tests check the calculated times against Aladhan, and a CI workflow builds and tests every push.

## [1.0.1] - 2026-11-16

### Fixed
- Dropdowns, number boxes and text boxes in Settings now follow the app theme instead of the stock white Windows controls.
- Toggles and other controls line up on one edge in every row, in English and Arabic.
- Settings cards no longer clip their right border.

## [1.0.0] - 2026-11-16

### Added
- Taskbar widget beside the notification area with the next prayer and a live countdown; it steps aside for other taskbar tools such as TrafficMonitor.
- Widget position: beside the tray or on the left, with a pixel shift.
- Prayer card with all six times, progress to the next prayer, Gregorian and Hijri dates, and the sunnah rawatib per prayer.
- Day navigation (arrows, mouse wheel, keys) and a themed calendar with Hijri days.
- Reminders: notification at prayer time, early reminder, flashing widget, optional adhan with a built-in recording or a custom file.
- Automatic location (Windows location, then IP lookup) or manual city search.
- Nine calculation methods, Shafi'i or Hanafi Asr, Hijri date adjustment.
- English and Arabic interface with a full right-to-left layout.
- Settings window in the Windows 11 style, following the system light or dark theme.
- Automatic updates from GitHub Releases with SHA-256 verification.
- Windows 10 support: the task-button strip is shrunk to make room for the widget.

[1.6.7]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.6.7
[1.6.6]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.6.6
[1.6.5]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.6.5
[1.6.4]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.6.4
[1.6.3]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.6.3
[1.6.2]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.6.2
[1.6.1]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.6.1
[1.6.0]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.6.0
[1.5.0]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.5.0
[1.4.4]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.4.4
[1.4.3]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.4.3
[1.4.2]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.4.2
[1.4.1]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.4.1
[1.4.0]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.4.0
[1.3.0]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.3.0
[1.2.1]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.2.1
[1.2.0]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.2.0
[1.1.1]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.1.1
[1.1.0]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.1.0
[1.0.5]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.0.5
[1.0.4]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.0.4
[1.0.3]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.0.3
[1.0.2]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.0.2
[1.0.1]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.0.1
[1.0.0]: https://github.com/medovanx/salaty-reminder/releases/tag/v1.0.0
