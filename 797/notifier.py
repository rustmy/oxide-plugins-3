import re
import Rust
import BasePlayer
import server
import UnityEngine.Random as random
from System import Action, Int32, String

# GLOBAL VARIABLES
DEV = False
LATEST_CFG = 3.2
LINE = '-' * 50

class notifier:

    # ==========================================================================
    # <>> PLUGIN
    # ==========================================================================
    def __init__(self):

        # PLUGIN INFO
        self.Title = 'Notifier'
        self.Version = V(2, 5, 0)
        self.Author = 'SkinN'
        self.Description = 'Broadcasts chat messages as notifications and advertising.'
        self.HasConfig = True
        self.ResourceId = 797

    # ==========================================================================
    # <>> CONFIGURATION
    # ==========================================================================
    def LoadDefaultConfig(self):

        # DICTIONARY
        self.Config = {
            'CONFIG_VERSION': LATEST_CFG,
            'SETTINGS': {
                'PREFIX': 'NOTIFIER',
                'BROADCAST TO CONSOLE': True,
                'OWNER TAG': '[OWNER]',
                'MODERATOR TAG': '[MOD]',
                'SHOW CONNECTED': True,
                'SHOW DISCONNECTED': True,
                'RULES LANGUAGE': 'AUTO',
                'HIDE ADMINS CONNECTIONS': False,
                'CHAT PLAYERS LIST': True,
                'CONSOLE PLAYERS LIST': True,
                'ADVERTS INTERVAL': 300,
                'ENABLE WELCOME MESSAGE': True,
                'ENABLE ADVERTS': True,
                'ENABLE HELPTEXT': True,
                'ENABLE ADMIN TAGS': False,
                'ENABLE SEED CMD': False,
                'ENABLE PLAYERS LIST CMD': True,
                'ENABLE ADMINS LIST CMD': False,
                'ENABLE PLUGINS LIST CMD': False,
                'ENABLE RULES CMD': True
            },
            'MESSAGES': {
                'CONNECTED': '{username} connected from {country}.',
                'DISCONNECTED': '{username} disconnected.',
                'SERVER SEED': 'The server seed is: {seed}',
                'NO ADMINS ONLINE': 'There are no Admins currently online.',
                'ONLY PLAYER': 'You are the only survivor online.',
                'CHECK CONSOLE NOTE': 'Check the console (press F1) for more info.',
                'PLAYERS COUNT': 'There are {count} survivors online.',
                'NO RULES': 'No rules have been found! Contact the Admins.',
                'ADMINS LIST TITLE': 'ADMINS ONLINE',
                'PLUGINS LIST TITLE': 'SERVER PLUGINS',
                'PLAYERS LIST TITLE': 'PLAYERS ONLINE',
                'RULES TITLE': 'SERVER RULES',
                'PLAYERS LIST DESC': '/players - Lists all the players either on the chat or console.',
                'ADMINS LIST DESC': '/admins - Lists all the Admins currently online in the chat.',
                'PLUGINS LIST DESC': '/plugins - Lists all the server plugins in the chat.',
                'RULES DESC': '/rules - Lists the server rules on the chat.',
                'SEED DESC': '/seed - Prints the current server seed on the chat. (Unless it is Random)'
            },
            'WELCOME MESSAGE': (
                'Welcome [cyan]{username}[/end], to our server!',
                'Type /help for all available commands.',
                'SERVER IP: [cyan]{ip}:{port}[/end]'
            ),
            'ADVERTS': (
                '[lime]Want to know the available commands?[/end] Type /help.',
                'Respect the server [red]/rules[/end].',
                'This server is running [orange]Oxide 2[/end].',
                'Cheating is strictly prohibited.'
            ),
            'COLORS': {
                'PREFIX': 'red',
                'CONNECTED MESSAGE': 'lime',
                'DISCONNECTED MESSAGE': 'lime',
                'WELCOME MESSAGE': '#0099FF',
                'ADVERTS': 'yellow',
                'OWNER NAME': 'white',
                'MODERATOR NAME': 'white'
            },
            'COMMANDS': {
                'PLAYERS LIST': 'players',
                'RULES': 'rules',
                'PLUGINS LIST': 'plugins',
                'SEED': 'seed',
                'ADMINS LIST': 'admins'
            },
            'SIZE': {
                'PREFIX': 10,
                'CONNECTED': 10,
                'DISCONNECTED': 10,
                'ADVERTS': 10,
                'WELCOME MESSAGE': 10
            },
            'RULES': {
                'EN': (
                    'Cheating is strictly prohibited.',
                    'Respect all players',
                    'Avoid spam in chat.',
                    'Play fair and don\'t abuse of bugs/exploits.'
                ),
                'PT': (
                    'Usar cheats e totalmente proibido.',
                    'Respeita todos os jogadores.',
                    'Evita spam no chat.',
                    'Nao abuses de bugs ou exploits.'
                ),
                'FR': (
                    'Tricher est strictement interdit.',
                    'Respectez tous les joueurs.',
                    'Évitez le spam dans le chat.',
                    'Jouer juste et ne pas abuser des bugs / exploits.'
                ),
                'ES': (
                    'Los trucos están terminantemente prohibidos.',
                    'Respeta a todos los jugadores.',
                    'Evita el Spam en el chat.',
                    'Juega limpio y no abuses de bugs/exploits.'
                ),
                'DE': (
                    'Cheaten ist verboten!',
                    'Respektiere alle Spieler',
                    'Spam im Chat zu vermeiden.',
                    'Spiel fair und missbrauche keine Bugs oder Exploits.'
                ),
                'TR': (
                    'Hile kesinlikle yasaktır.',
                    'Tüm oyuncular Saygı.',
                    'Sohbet Spam kaçının.',
                    'Adil oynayın ve böcek / açıkları kötüye yok.'
                ),
                'IT': (
                    'Cheating è severamente proibito.',
                    'Rispettare tutti i giocatori.',
                    'Evitare lo spam in chat.',
                    'Fair Play e non abusare di bug / exploit.'
                ),
                'DK': (
                    'Snyd er strengt forbudt.',
                    'Respekt alle spillere.',
                    'Undgå spam i chatten.',
                    'Play fair og ikke misbruger af bugs / exploits.'
                ),
                'RU': (
                    'Запрещено использовать читы.',
                    'Запрещено спамить и материться.',
                    'Уважайте других игроков.',
                    'Играйте честно и не используйте баги и лазейки.'
                ),
                'NL': (
                    'Vals spelen is ten strengste verboden.',
                    'Respecteer alle spelers',
                    'Vermijd spam in de chat.',
                    'Speel eerlijk en maak geen misbruik van bugs / exploits.'
                ),
                'UA': (
                    'Обман суворо заборонено.',
                    'Поважайте всіх гравців',
                    'Щоб уникнути спаму в чаті.',
                    'Грати чесно і не зловживати помилки / подвиги.'
                )
            }
        }

        self.console('Loading default configuration file', True)

    # --------------------------------------------------------------------------
    def UpdateConfig(self):

        # IS OLDER CONFIG TWO VERSIONS OLDER?
        if (self.Config['CONFIG_VERSION'] <= LATEST_CFG - 0.2) or DEV:

            self.console('Current configuration file is two or more versions older than the latest (Current: v%s / Latest: v%s)' % (self.Config['CONFIG_VERSION'], LATEST_CFG), True)
            
            # RESET CONFIGURATION
            self.Config.clear()

            # LOAD THE DEFAULT CONFIGURATION
            self.LoadDefaultConfig()

        else:

            self.console('Applying changes of the new configuration file version', True)

            # NEW VERSION VALUE
            self.Config['CONFIG_VERSION'] = LATEST_CFG

            # NEW CHANGES
            self.Config['SIZE'] = {
                'PREFIX': 10,
                'CONNECTED': 10,
                'DISCONNECTED': 10,
                'ADVERTS': 10,
                'WELCOME MESSAGE': 10
            }

        # SAVE CHANGES
        self.SaveConfig()

    # ==========================================================================
    # <>> PLUGIN SPECIFIC
    # ==========================================================================
    def Init(self):

        self.console('Loading Plugin')
        self.console(LINE)

        # UPDATE CONFIG FILE
        if self.Config['CONFIG_VERSION'] < LATEST_CFG or DEV:

            self.UpdateConfig()

        # CONFIGURATION VARIABLES
        global MSG, PLUGIN, COLOR, SIZE
        SIZE = self.Config['SIZE']
        MSG = self.Config['MESSAGES']
        COLOR = self.Config['COLORS']
        PLUGIN = self.Config['SETTINGS']

        # PLUGIN SPECIFIC
        self.prefix = '<size=%s><color=%s>%s</color></size>' % (SIZE['PREFIX'], COLOR['PREFIX'], PLUGIN['PREFIX']) if PLUGIN['PREFIX'] else None
        self.title = '<color=red>%s</color>' % self.Title.upper()
        self.countries = {}
        self.lastadvert = 0

        # GET CONNECTED PLAYERS COUNTRIES
        for player in self.player_list():

            self.get_country(self.get_player(player), False)

        # IS ADVERTS ENABLED?
        if PLUGIN['ENABLE ADVERTS']:

            sec = PLUGIN['ADVERTS INTERVAL']

            if sec < 60:

                self.console('Adverts interval can\'t be lower than 60 seconds, setting to default value. (Current: %s second/s)' % sec)

            sec = sec if sec > 59 else 300

            self.adverts_loop = timer.Repeat(sec, 0, Action(self.send_advert), self.Plugin)

            self.console('Adverts are enabled, starting messages loop (Interval: %d minute/s %d second/s)' % divmod(sec, 60))

        else:

            self.adverts_loop = None

            self.console('Adverts are disabled.')

        # COMMANDS
        self.cmds = []

        for cmd in ('PLAYERS LIST', 'RULES', 'ADMINS LIST', 'PLUGINS LIST', 'SEED'):

            # IS COMMAND ENABLED?
            if PLUGIN['ENABLE %s CMD' % cmd]:

                self.cmds.append(cmd)

                command.AddChatCommand(self.Config['COMMANDS'][cmd], self.Plugin, '%s_CMD' % cmd.replace(' ', '_').lower())

        n = '%s' % self.Title.lower()
        command.AddChatCommand(n, self.Plugin, '%s_CMD' % n)

        self.console('Enabled commands:')

        if self.cmds:

            for cmd in self.cmds:

                self.console('- /%s (%s)' % (self.Config['COMMANDS'][cmd], cmd.title()))

        else:

            self.console('- No commands enabled')

        self.console(LINE)
        self.console('Loading Complete')

    # --------------------------------------------------------------------------
    def Unload(self):

        # STOP ADVERTS LOOP
        if self.adverts_loop:

            self.adverts_loop.Destroy()

            self.console('Stopping Adverts loop')

        self.console('Unload complete')

    # ==========================================================================
    # <>> MESSAGE FUNTIONS
    # ==========================================================================
    def console(self, text, force=False):
        ''' Sends a console message '''

        if self.Config['SETTINGS']['BROADCAST TO CONSOLE'] or force:

            print('[%s v%s] :: %s' % (self.Title, str(self.Version), self._format(text, True)))

    # --------------------------------------------------------------------------
    def pconsole(self, player, text, color='white'):
        ''' Sends a message to a player console '''

        player.SendConsoleCommand('echo <color=%s>%s</color>' % (color, text))

    # --------------------------------------------------------------------------
    def say(self, text, color='white', size=10, userid=0, force=True):
        ''' Sends a global chat message '''

        if self.prefix and force:

            string = '%s <color=white>:</color> <size=%s><color=%s>%s</color></size>' % (self.prefix, size, color, self._format(text))

            rust.BroadcastChat(string, None, str(userid))

        else:

            rust.BroadcastChat('<size=%s><color=%s>%s</color></size>' % (size, color, self._format(text)), None, str(userid))

        self.console(self._format(text, True))

    # --------------------------------------------------------------------------
    def tell(self, player, text, color='white', userid=0, force=True):
        ''' Sends a global chat message '''

        if self.prefix and force:

            rust.SendChatMessage(player, '%s <color=white>:</color> <color=%s>%s</color>' % (self.prefix, color, self._format(text)), None, str(userid))

        else:

            rust.SendChatMessage(player, '<color=%s>%s</color>' % (color, self._format(text)), None, str(userid))

    # --------------------------------------------------------------------------
    def _format(self, text, con=False):

        if con:

            for x in (r'\[(\w+)\]', r'\[#(\w+)\]'):

                text = re.sub(x, '', text)

            text = text.replace('[/end]', '')

        else:

            # REPLACE COLOR NAMES
            for x in re.findall(r'\[(\w+)\]', text):

                text = text.replace('[%s]' % x, '<color=%s>' % x)

            # REPLACE HEX CODES
            for x in re.findall(r'\[#(\w+)\]', text):

                text = text.replace('[#%s]' % x, '<color=#%s>' % x)

            # REPLACE COLOR ENDINGS
            text = text.replace('[/end]', '</color>')

        return text

    # ==========================================================================
    # <>> HOOKS
    # ==========================================================================
    def OnPlayerInit(self, player):

        target = self.get_player(player)

        # CONNECTED MESSAGE / GET PLAYER COUNTRY
        self.get_country(target)

        # ADMINS TAGS / SHOULD USE ADMIN TAGS?
        if PLUGIN['ENABLE ADMIN TAGS']:

            if target['auth'] == 1:

                player.displayName = '%s %s' % (PLUGIN['MODERATOR TAG'], target['username'])

            elif target['auth'] == 2:

                player.displayName = '%s %s' % (PLUGIN['OWNER TAG'], target['username'])

        # WELCOME MESSAGE / SHOULD DISPLAY WELCOME MESSAGE?
        if PLUGIN['ENABLE WELCOME MESSAGE']:

            l = self.Config['WELCOME MESSAGE']

            if l:

                for line in l:

                    line = line.format(ip=str(server.port), port=str(server.ip), seed=str(server.seed) if server.seed else 'Random', username=player.displayName)

                    self.tell(player, line, COLOR['WELCOME MESSAGE'], SIZE['WELCOME MESSAGE'])

            else:

                self.console('Welcome Message list is empty, turning Welcome Message off.')

                PLUGIN['ENABLE WELCOME MESSAGE'] = False

    # --------------------------------------------------------------------------
    def OnPlayerDisconnected(self, player):

        target = self.get_player(player)

        # CHECK IF PLAYER IS IN CONNECTED LIST
        if target['steamid'] in self.countries:

            # DISCONNECTED MESSAGE / SHOW CONNECTED MESSAGES?
            if PLUGIN['SHOW DISCONNECTED']:

                # SHOULD HIDE MESSAGE IF PLAYER IS AN ADMIN?
                if not (PLUGIN['HIDE ADMINS CONNECTIONS'] and int(target['auth']) > 0):

                    text = MSG['DISCONNECTED'].format(country=target['country'], username=target['username'], steamid=target['steamid'])

                    self.say(text, COLOR['DISCONNECTED MESSAGE'], SIZE['DISCONNECTED'], target['steamid'])

            # REMOVE FROM THE COUNTRIES DICTIONARY
            if target['steamid'] in self.countries:

                del self.countries[target['steamid']]

    # ==========================================================================
    # <>> MAIN FUNTIONS
    # ==========================================================================
    def send_advert(self):

        l = self.Config['ADVERTS']

        if l:

            index = self.lastadvert

            count = len(l)

            if count > 1:

                while index == self.lastadvert:

                    index = random.Range(0, len(l))

                self.lastadvert = index

            line = l[index].format(ip=str(server.port), port=str(server.ip), seed=str(server.seed) if server.seed else 'Random')

            self.say(line, COLOR['ADVERTS'], SIZE['ADVERTS'])

        else:

            self.console('The Adverts list is empty, stopping Adverts loop')

            self.adverts_loop.Destroy()

    # ==========================================================================
    # <>> OTHER FUNTIONS
    # ==========================================================================
    def seed_CMD(self, player, cmd, args):

        seed = str(server.seed) if server.seed else 'Random'
        text = MSG['SERVER SEED'].format(seed='<color=lime>%s</color>' % seed)

        self.tell(player, text)

    # --------------------------------------------------------------------------
    def admins_list_CMD(self, player, cmd, args):

        sort = ['<color=cyan>%s</color>' % i.displayName for i in player.activePlayerList if i.IsAdmin()]
        sort = [sort[i:i + 5] for i in range(0, len(sort), 6)]

        if sort:

            self.tell(player, '%s | %s:' % (self.title, MSG['ADMINS LIST TITLE']), force=False)
            self.tell(player, LINE, force=False)

            for i in sort:

                self.tell(player, ', '.join(i), 'white', force=False)

            self.tell(player, LINE, force=False)

        else:

            self.tell(player, MSG['NO ADMINS ONLINE'], 'yellow')

    # --------------------------------------------------------------------------
    def plugins_list_CMD(self, player, cmd, args):

        self.tell(player, '%s | %s:' % (self.title, MSG['PLUGINS LIST TITLE']), force=False)
        self.tell(player, LINE, force=False)

        for i in plugins.GetAll():

            if i.Author != 'Oxide Team':

                self.tell(player, '<color=lime>{plugin.Title} v{plugin.Version}</color> by {plugin.Author}'.format(plugin=i), force=False)
        
        self.tell(player, LINE, force=False)

    # --------------------------------------------------------------------------
    def players_list_CMD(self, player, cmd, args):

        l = self.player_list()

        count_msg = MSG['PLAYERS COUNT'].format(count='<color=lime>%s</color>' % len(l)) if len(l) > 1 else MSG['ONLY PLAYER']
        title = '%s | %s:' % (self.title, MSG['PLAYERS LIST TITLE'])
        chat = PLUGIN['CHAT PLAYERS LIST']
        console = PLUGIN['CONSOLE PLAYERS LIST']

        if chat:

            names = ['<color=lime>%s</color>' % x.displayName for x in l]
            names = [names[i:i + 4] for i in range(0, len(names), 6)]

            self.tell(player, title, force=False)
            self.tell(player, LINE, force=False)

            for i in names:

                self.tell(player, ', '.join(i), 'white', force=False)

            self.tell(player, LINE, force=False)
            self.tell(player, count_msg, 'yellow', force=False)

            if console:

                self.tell(player, '(%s)' % MSG['CHECK CONSOLE NOTE'], 'yellow', force=False)

            self.tell(player, LINE, force=False)

        if console:

            if not chat:

                self.tell(player, count_msg, 'yellow')
                self.tell(player, '(%s)' % MSG['CHECK CONSOLE NOTE'], 'yellow')

            self.pconsole(player, LINE)
            self.pconsole(player, title)
            self.pconsole(player, LINE)

            for num, ply in enumerate(l):

                self.pconsole(player, '<color=orange>{num}.</color> {country} | {steamid} | <color=lime>{username}</color>'.format(num='%03d' % (num + 1), **self.get_player(ply)))

            self.pconsole(player, LINE)
            self.pconsole(player, count_msg, 'yellow')
            self.pconsole(player, LINE)

    # --------------------------------------------------------------------------
    def rules_CMD(self, player, cmd, args):

        lang = self.get_lang(player)
        l = self.Config['RULES'][lang]

        if l:

            self.tell(player, '%s | %s:' % (self.title, MSG['RULES TITLE']), force=False)
            self.tell(player, LINE, force=False)

            if PLUGIN['RULES LANGUAGE'] != 'AUTO':

                self.tell(player, 'DISPLAYING RULES IN: %s' % PLUGIN['RULES LANGUAGE'], 'yellow', force=False)

            for num, line in enumerate(l):

                self.tell(player, '%s. %s' % (num + 1, line), 'orange', force=False)

            self.tell(player, LINE, force=False)

        else:

            self.tell(player, MSG['NO RULES'], 'yellow')

    # --------------------------------------------------------------------------
    def notifier_CMD(self, player, cmd, args):

        self.tell(player, LINE, force=False)
        self.tell(player, '<color=lime>%s v%s</color> by <color=lime>SkinN</color>' % (self.title, self.Version), force=False)
        self.tell(player, self.Description, 'lime', force=False)
        self.tell(player, '| RESOURSE ID: <color=lime>%s</color> | CONFIG: v<color=lime>%s</color> |' % (self.ResourceId, self.Config['CONFIG_VERSION']), force=False)
        self.tell(player, LINE, force=False)
        self.tell(player, '<< Click the icon to contact me.', userid='76561197999302614', force=False)

    # ==========================================================================
    # <>> OTHER FUNTIONS
    # ==========================================================================
    def get_player(self, player):
        ''' Returns a dictionary with player info: '''

        steamid = rust.UserIDFromPlayer(player)
        connection = player.net.connection

        return {
            'username': player.displayName,
            'steamid': steamid,
            'auth': connection.authLevel,
            'con': connection,
            'country': self.countries[steamid] if steamid in self.countries else 'Unknown'
        }

    # --------------------------------------------------------------------------
    def player_list(self):
        ''' Returns the server player list '''

        return BasePlayer.activePlayerList

    # --------------------------------------------------------------------------
    def get_country(self, target, send=True):
        ''' Webresquest to get the player country '''

        ip = target['con'].ipaddress.split(':')[0]
        country = 'undefined'

        # WEBREQUEST FUNTION
        def response_handler(code, response):

            country = response.replace('\n','')

            if country == 'undefined' or code != 200:

                country = 'Unknown'

            # SAVE COUNTRY
            self.countries[target['steamid']] = country

            if send:

                # CONNECTED MESSAGE / SHOW CONNECTED MESSAGES?
                if PLUGIN['SHOW CONNECTED']:

                    # SHOULD HIDE MESSAGE IF PLAYER IS AN ADMIN?
                    if not (PLUGIN['HIDE ADMINS CONNECTIONS'] and int(target['auth']) > 0):

                        text = MSG['CONNECTED'].format(country=country, username=target['username'], steamid=target['steamid'])

                        self.say(text, COLOR['CONNECTED MESSAGE'], SIZE['CONNECTED'], target['steamid'])

        # WEBRESQUET
        webrequests.EnqueueGet('http://ipinfo.io/%s/country' % ip, Action[Int32,String](response_handler), self.Plugin)

    # --------------------------------------------------------------------------
    def get_lang(self, player):
        ''' Filter for rule's languages '''

        default = PLUGIN['RULES LANGUAGE']

        if default == 'AUTO':

            steamid = rust.UserIDFromPlayer(player)
            lang = self.countries[steamid] if steamid in self.countries else 'EN'

            # PORTGUESE FILTER
            if lang in ('PT','BR'): lang = 'PT'
            # SPANISH FILTER
            elif lang in ('ES','MX','AR'): lang = 'ES'
            # FRENCH FILTER
            elif lang in ('FR','BE','CH','MC','MU'): lang = 'FR'

            return lang if lang in self.Config['RULES'] else 'EN'

        else:

            return default if default in self.Config['RULES'] else 'EN'

    # ==========================================================================
    # <>> HELP TEXT
    # ==========================================================================
    def SendHelpText(self, player, cmd=None, args=None):

        # IS HELPTEXT ENABLED?
        if PLUGIN['ENABLE HELPTEXT']:

            for cmd in self.cmds:

                self.tell(player, MSG['%s DESC' % cmd], 'lime')

# ==============================================================================