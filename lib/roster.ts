export type AgentStatus = 'active' | 'recruiting';

export interface Agent {
  id: number;
  name: string;
  role: string;
  tag: string;
  position?: string;
  color: string;
  status: AgentStatus;
  discord?: string;
  dotaId?: string;
  rank?: string;
  email?: string;
}

export const roster: Agent[] = [
  {
    id: 1,
    name: 'LUCKER23',
    role: 'Manager',
    tag: 'MGR',
    color: '#a855f7',
    status: 'active',
    discord: 'lucker23',
    email: 'lucker23@nonek.online',
  },
  {
    id: 2,
    name: 'NONEK',
    role: 'MVP Carry',
    tag: 'MVP',
    position: 'Pos 1',
    color: '#e8a020',
    status: 'active',
    discord: 'nonek',
    dotaId: 'TBD',
    rank: 'Immortal',
    email: 'nonek@nonek.online',
  },
  {
    id: 3,
    name: 'CHILLZER',
    role: 'Coach',
    tag: 'COACH',
    color: '#00e5ff',
    status: 'active',
    discord: 'chillzer',
    email: 'chillzer@nonek.online',
  },
  {
    id: 4,
    name: 'AGENT-04',
    role: 'Mid',
    tag: 'MID',
    position: 'Pos 2',
    color: '#f43f5e',
    status: 'recruiting',
  },
  {
    id: 5,
    name: 'AGENT-05',
    role: 'Offlane',
    tag: 'OFF',
    position: 'Pos 3',
    color: '#10b981',
    status: 'recruiting',
  },
  {
    id: 6,
    name: 'AGENT-06',
    role: 'Soft Support',
    tag: 'P4',
    position: 'Pos 4',
    color: '#3b82f6',
    status: 'recruiting',
  },
  {
    id: 7,
    name: 'AGENT-07',
    role: 'Hard Support',
    tag: 'P5',
    position: 'Pos 5',
    color: '#06b6d4',
    status: 'recruiting',
  },
  {
    id: 8,
    name: 'AGENT-08',
    role: 'Data Analyst',
    tag: 'DATA',
    color: '#8b5cf6',
    status: 'recruiting',
  },
  {
    id: 9,
    name: 'AGENT-09',
    role: 'Content Creator',
    tag: 'CC',
    color: '#f59e0b',
    status: 'recruiting',
  },
  {
    id: 10,
    name: 'AGENT-10',
    role: 'Substitute',
    tag: 'SUB',
    color: '#64748b',
    status: 'recruiting',
  },
];

export const activeAgents = roster.filter((a) => a.status === 'active');
export const recruitingAgents = roster.filter((a) => a.status === 'recruiting');
